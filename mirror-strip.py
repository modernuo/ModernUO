"""Strip C# comments for the UOBot mirror, keeping only the XML documentation directly above a type.

Usage:
    python mirror-strip.py strip <src_root> <dst_root>   strip every .cs under src_root into dst_root (same tree)
    python mirror-strip.py check <orig_root> <strip_root> token-compare and count what is left
    python mirror-strip.py one <file>                    print the stripped text of one file

A lexer, not regular expressions: comment markers inside strings ("http://..."), verbatim, interpolated
(with nested strings in holes), raw and char literals must survive untouched.
"""

import os
import re
import sys

TYPE_KW = {"class", "struct", "record", "interface", "enum", "delegate"}
MODIFIERS = {
    "public", "internal", "private", "protected", "static", "sealed", "abstract", "partial", "readonly",
    "ref", "unsafe", "file", "new", "extern", "override", "virtual", "required",
}
WS_CHARS = " \t\r\n\f\v"
LF = chr(10)
CRLF = chr(13) + chr(10)
IDENT = re.compile(r"[A-Za-z0-9_\u0080-￿]+")


class Tok:
    __slots__ = ("kind", "text")

    def __init__(self, kind, text):
        self.kind = kind
        self.text = text

    def __repr__(self):
        return f"{self.kind}:{self.text!r}"


def scan_char(src, i):
    """src[i] == "'" -> index after the closing quote."""
    n = len(src)
    j = i + 1
    if j < n and src[j] == "\\":
        j += 2
        while j < n and src[j] != "'":
            j += 1
        return j + 1
    # one character (or a surrogate pair), then the quote
    j += 1
    while j < n and src[j] != "'" and j - i < 4:
        j += 1
    return j + 1


def scan_hole(src, i, raw_braces=1):
    """src[i] is the first char inside a hole; returns index after the hole's closing brace(s)."""
    n = len(src)
    depth = 0
    j = i
    while j < n:
        c = src[j]
        if c in "\"$@" and is_string_start(src, j):
            j = scan_string(src, j)
            continue
        if c == "'":
            j = scan_char(src, j)
            continue
        if c == "/" and j + 1 < n and src[j + 1] == "*":
            k = src.find("*/", j + 2)
            j = n if k < 0 else k + 2
            continue
        if c in "([{":
            depth += 1
        elif c in ")]":
            depth -= 1
        elif c == "}":
            if depth == 0:
                if raw_braces > 1:
                    if src[j:j + raw_braces] == "}" * raw_braces:
                        return j + raw_braces
                else:
                    return j + 1
            else:
                depth -= 1
        j += 1
    return n


def is_string_start(src, i):
    n = len(src)
    j = i
    while j < n and src[j] in "$@":
        j += 1
    return j < n and src[j] == '"'


def scan_string(src, i):
    """src[i] starts a string literal (possibly with $/@ prefixes); returns index after it."""
    n = len(src)
    j = i
    dollars = 0
    verbatim = False
    while j < n and src[j] in "$@":
        if src[j] == "$":
            dollars += 1
        else:
            verbatim = True
        j += 1
    q = 0
    while j + q < n and src[j + q] == '"':
        q += 1
    if q >= 3:
        # raw string literal: ends at the first run of exactly q quotes
        start = j + q
        k = start
        while True:
            k = src.find('"' * q, k)
            if k < 0:
                return n
            run = q
            while k + run < n and src[k + run] == '"':
                run += 1
            # a longer run ends the literal with its last q quotes
            return k + run
    if q == 2:
        return j + 2  # "" empty string
    j += 1  # past the opening quote
    while j < n:
        c = src[j]
        if verbatim:
            if c == '"':
                if j + 1 < n and src[j + 1] == '"':
                    j += 2
                    continue
                return j + 1
        else:
            if c == "\\":
                j += 2
                continue
            if c == '"':
                return j + 1
            if c == "\n":
                return j  # unterminated; let the compiler complain
        if dollars and c == "{":
            if j + 1 < n and src[j + 1] == "{":
                j += 2
                continue
            j = scan_hole(src, j + 1)
            continue
        if dollars and c == "}" and j + 1 < n and src[j + 1] == "}":
            j += 2
            continue
        j += 1
    return n


def lex(src):
    toks = []
    i = 0
    n = len(src)
    if src.startswith("﻿"):
        toks.append(Tok("bom", "﻿"))
        i = 1
    while i < n:
        c = src[i]
        if c in WS_CHARS:
            j = i
            while j < n and src[j] in WS_CHARS:
                j += 1
            toks.append(Tok("ws", src[i:j]))
            i = j
            continue
        if c == "/" and i + 1 < n and src[i + 1] == "/":
            j = i
            while j < n and src[j] not in "\r\n":
                j += 1
            text = src[i:j]
            kind = "doc" if text.startswith("///") and not text.startswith("////") else "line"
            toks.append(Tok(kind, text))
            i = j
            continue
        if c == "/" and i + 1 < n and src[i + 1] == "*":
            k = src.find("*/", i + 2)
            j = n if k < 0 else k + 2
            toks.append(Tok("block", src[i:j]))
            i = j
            continue
        if c in "\"$@" and is_string_start(src, i):
            j = scan_string(src, i)
            toks.append(Tok("str", src[i:j]))
            i = j
            continue
        if c == "'":
            j = scan_char(src, i)
            toks.append(Tok("chr", src[i:j]))
            i = j
            continue
        m = IDENT.match(src, i)
        if m:
            toks.append(Tok("code", m.group(0)))
            i = m.end()
            continue
        toks.append(Tok("code", c))
        i += 1
    assert "".join(t.text for t in toks) == src, "lexer lost text"
    return toks


COMMENT = {"line", "doc", "block"}


def next_sig(toks, k):
    """Index of the next token that is neither whitespace nor a comment, from k."""
    while k < len(toks) and (toks[k].kind == "ws" or toks[k].kind in COMMENT):
        k += 1
    return k


def doc_is_above_type(toks, k):
    """k is the index just past a doc block."""
    k = next_sig(toks, k)
    # attributes
    while k < len(toks) and toks[k].kind == "code" and toks[k].text == "[":
        depth = 0
        while k < len(toks):
            t = toks[k]
            if t.kind == "code" and t.text == "[":
                depth += 1
            elif t.kind == "code" and t.text == "]":
                depth -= 1
                if depth == 0:
                    k += 1
                    break
            k += 1
        k = next_sig(toks, k)
    while k < len(toks) and toks[k].kind == "code" and toks[k].text in MODIFIERS:
        k = next_sig(toks, k + 1)
    return k < len(toks) and toks[k].kind == "code" and toks[k].text in TYPE_KW


def mark(toks):
    """Returns a set of token indices to drop."""
    drop = set()
    i = 0
    n = len(toks)
    while i < n:
        t = toks[i]
        if t.kind in ("line", "block"):
            drop.add(i)
            i += 1
            continue
        if t.kind == "doc":
            block = [i]
            k = i + 1
            while k + 1 < n and toks[k].kind == "ws" and toks[k].text.count("\n") == 1 and toks[k + 1].kind == "doc":
                block.append(k + 1)
                k += 2
            if not doc_is_above_type(toks, block[-1] + 1):
                drop.update(block)
            i = block[-1] + 1
            continue
        i += 1
    return drop


def strip(src):
    toks = lex(src)
    drop = mark(toks)
    nl = CRLF if CRLF in src else LF
    out = []
    n = len(toks)
    i = 0
    while i < n:
        t = toks[i]
        if t.kind != "ws" and i not in drop:
            out.append(t.text)
            i += 1
            continue
        # a run of whitespace and dropped comments
        j = i
        while j < n and (toks[j].kind == "ws" or j in drop):
            j += 1
        run = range(i, j)
        if not any(k in drop for k in run):
            text = "".join(toks[k].text for k in run)
            if text.count("\n") > 2 and j < n:
                # never more than one blank line in a row, as the mirror has always been written
                text = nl * 2 + text[text.rfind("\n") + 1:]
            out.append(text)
            i = j
            continue
        prev = None
        for k in range(i - 1, -1, -1):
            if toks[k].kind != "ws":
                prev = toks[k]
                break
        nxt = toks[j] if j < n else None
        at_start = prev is None or prev.kind == "bom"
        at_end = nxt is None
        # Split the run into lines. The first shares a line with the token before the run, the last with the
        # token after it; a line wholly inside the run that held a dropped comment is removed with its newline.
        segments = [[False, ""]]  # [held a dropped comment, whitespace before the first comment on it]
        for k in run:
            tk = toks[k]
            if tk.kind == "ws":
                parts = tk.text.split("\n")
                for p_idx, part in enumerate(parts):
                    if p_idx > 0:
                        segments.append([False, ""])
                    if not segments[-1][0]:
                        segments[-1][1] += part
            else:
                segments[-1][0] = True
        newlines = len(segments) - 1
        removed = sum(1 for s in segments[1:-1] if s[0])
        indent = segments[-1][1].replace("\r", "")
        r = newlines - removed
        if at_start:
            r = 0
            indent = ""
        if prev is not None and prev.kind == "code" and prev.text == "{":
            r = min(r, 1)
        if nxt is not None and nxt.kind == "code" and nxt.text == "}":
            r = min(r, 1)
        r = min(r, 2)
        if at_end:
            out.append(nl if r >= 1 or newlines >= 1 else "")
        elif r >= 1:
            out.append(nl * r + indent)
        else:
            # same line: keep one space between two tokens that would otherwise touch
            need = prev is not None and nxt is not None and not at_start
            out.append(" " if need else "")
        i = j
    return "".join(out)


def sig(toks):
    return [(t.kind, t.text) for t in toks if t.kind in ("code", "str", "chr", "bom")]


def main():
    cmd = sys.argv[1]
    sys.stdout.reconfigure(encoding="utf-8")
    if cmd == "one":
        with open(sys.argv[2], encoding="utf-8", newline="") as f:
            sys.stdout.write(strip(f.read()))
        return
    if cmd == "calib":
        # calib <list file> <src_root> <mirror_root>: strip each listed file and compare with the mirror's copy
        import difflib
        names = [l.strip() for l in open(sys.argv[2], encoding="utf-8") if l.strip()]
        same = 0
        shown = 0
        differing = []
        for name in names:
            with open(os.path.join(sys.argv[3], name), encoding="utf-8", newline="") as f:
                mine = strip(f.read())
            with open(os.path.join(sys.argv[4], name), encoding="utf-8", newline="") as f:
                theirs = f.read()
            if mine == theirs:
                same += 1
                continue
            differing.append(name)
            if shown < 4:
                shown += 1
                d = list(difflib.unified_diff(theirs.splitlines(), mine.splitlines(), "mirror/" + name, "mine/" + name, n=1, lineterm=""))
                print("\n".join(d[:30]))
        print(f"identical: {same} of {len(names)}; differing: {len(differing)}")
        for name in differing[:40]:
            print("  ", name)
        return
    if cmd == "strip":
        src_root, dst_root = sys.argv[2], sys.argv[3]
        count = 0
        for dirpath, dirnames, filenames in os.walk(src_root):
            dirnames[:] = [d for d in dirnames if d not in ("obj", "bin", "__pycache__")]
            for name in filenames:
                if not name.endswith(".cs"):
                    continue
                s = os.path.join(dirpath, name)
                d = os.path.join(dst_root, os.path.relpath(s, src_root))
                with open(s, encoding="utf-8", newline="") as f:
                    text = f.read()
                os.makedirs(os.path.dirname(d), exist_ok=True)
                with open(d, "w", encoding="utf-8", newline="") as f:
                    f.write(strip(text))
                count += 1
        print(f"stripped {count} files")
        return
    if cmd == "check":
        orig_root, strip_root = sys.argv[2], sys.argv[3]
        bad = 0
        files = 0
        left_line = left_block = left_doc_not_type = kept_docs = 0
        for dirpath, dirnames, filenames in os.walk(orig_root):
            dirnames[:] = [d for d in dirnames if d not in ("obj", "bin", "__pycache__")]
            for name in filenames:
                if not name.endswith(".cs"):
                    continue
                o = os.path.join(dirpath, name)
                s = os.path.join(strip_root, os.path.relpath(o, orig_root))
                with open(o, encoding="utf-8", newline="") as f:
                    ot = lex(f.read())
                with open(s, encoding="utf-8", newline="") as f:
                    st = lex(f.read())
                files += 1
                if sig(ot) != sig(st):
                    bad += 1
                    a, b = sig(ot), sig(st)
                    k = next((x for x in range(min(len(a), len(b))) if a[x] != b[x]), min(len(a), len(b)))
                    print(f"TOKENS DIFFER: {s} at {k}: {a[k - 3:k + 3]} vs {b[k - 3:k + 3]}")
                sd = mark(st)
                for idx, t in enumerate(st):
                    if t.kind == "line":
                        left_line += 1
                    elif t.kind == "block":
                        left_block += 1
                    elif t.kind == "doc":
                        if idx in sd:
                            left_doc_not_type += 1
                        else:
                            kept_docs += 1
        print(f"{files} files; token sequences differing: {bad}; left: {left_line} line comments, {left_block} block comments, "
              f"{left_doc_not_type} doc lines not above a type; {kept_docs} doc lines kept above types")
        return


if __name__ == "__main__":
    main()

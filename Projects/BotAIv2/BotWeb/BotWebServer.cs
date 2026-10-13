using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The dashboard's door: a small HTTP server on the loopback address that hands out what the snapshot has
/// already written.
///
/// <para>
/// <b>Nothing here reads the world.</b> Every answer is a string the game loop prepared, or a file on disk,
/// or the event ring under its lock. That is what lets the listener run on the thread pool without a single
/// lock on the loop, and it is the rule that keeps a page refreshed every two seconds from costing the
/// population anything at all.
/// </para>
///
/// <para>
/// <b>Loopback by default.</b> The page can teleport nothing and change nothing, but it names every bot and
/// every guild's chest, and the shard is somebody's living room. Binding elsewhere is a setting, not a
/// default.
/// </para>
/// </summary>
public static class BotWebServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWebServer));

    public static string Bind { get; set; } = "127.0.0.1";

    public static int Port { get; set; } = 2599;

    /// <summary>Where the page lives, under the Distribution folder.</summary>
    public static string Folder { get; set; } = "Data/bot-web";

    public static bool Running { get; private set; }

    public static long Requests { get; private set; }

    public static long Errors { get; private set; }

    public static int Streams => _streams;

    private static int _streams;

    private static HttpListener _listener;

    private static CancellationTokenSource _stopping;

    private static string _page;

    private static DateTime _pageStamp;

    public static string Url => $"http://{Bind}:{Port}/";

    public static string FolderPath => Path.GetFullPath(Path.Combine(Core.BaseDirectory, Folder));

    public static void Start()
    {
        if (Running)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(FolderPath);

            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();
            _stopping = new CancellationTokenSource();
            Running = true;

            _ = Task.Run(Accept);

            logger.Information("The dashboard is listening at {Url}; the page is read from {Folder}", Url, FolderPath);
        }
        catch (Exception e)
        {
            Running = false;

            logger.Warning("The dashboard could not listen at {Url}: {Message}", Url, e.Message);
        }
    }

    public static void Stop()
    {
        if (!Running)
        {
            return;
        }

        Running = false;

        try
        {
            _stopping?.Cancel();
            _listener?.Stop();
            _listener?.Close();
        }
        catch
        {
            // Stopping is best effort.
        }
    }

    private static async Task Accept()
    {
        while (Running)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception) when (!Running)
            {
                return;
            }
            catch (Exception e)
            {
                Errors++;

                logger.Warning("The dashboard could not accept a request: {Message}", e.Message);

                await Task.Delay(200).ConfigureAwait(false);

                continue;
            }

            _ = Task.Run(() => Handle(context));
        }
    }

    private static void Handle(HttpListenerContext context)
    {
        Requests++;

        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath ?? "/";

        try
        {
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Cache-Control"] = "no-store";

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();

                return;
            }

            switch (path)
            {
                case "/":
                case "/index.html":
                    Text(response, Page(), "text/html; charset=utf-8");

                    return;
                case "/api/state":
                    Text(response, BotWebSnapshot.State, "application/json; charset=utf-8");

                    return;
                case "/api/paths":
                    Text(response, BotWebSnapshot.Paths, "application/json; charset=utf-8");

                    return;
                case "/api/craft":
                    Text(response, BotWebSnapshot.Craft, "application/json; charset=utf-8");

                    return;
                case "/api/roads":
                    Text(response, BotRoadbook.Json, "application/json; charset=utf-8");

                    return;
                case "/api/zones":
                    {
                        // The JSON and its tag are read together, once: a page that already holds this version is told so.
                        var zones = BotZones.Page;

                        response.Headers["ETag"] = zones.ETag;

                        if (request.Headers["If-None-Match"] == zones.ETag)
                        {
                            response.StatusCode = 304;
                            response.Close();

                            return;
                        }

                        Text(response, zones.Json, "application/json; charset=utf-8");
                    }

                    return;
                case "/api/history":
                    Text(response, BotWebHistory.Json(), "application/json; charset=utf-8");

                    return;
                case "/api/events":
                    Text(response, Events(request), "application/json; charset=utf-8");

                    return;
                case "/api/stream":
                    Stream(response);

                    return;
                case "/api/log":
                    Text(response, Log(request), "text/plain; charset=utf-8");

                    return;
                case "/api/map.png":
                    File(response, BotWebMap.Ready ? BotWebMap.Path : null, "image/png");

                    return;
                case "/api/health":
                    Text(response, BotWebSnapshot.Health(), "application/json; charset=utf-8");

                    return;

                // The live window (02.10.2026): the newest frame of one bot, which also marks it watched; the same as server-sent
                // events; and the debugger squad. All three strings the loop wrote. See BotWebLive.
                case "/api/live":
                    Live(response, request.QueryString["bot"]);

                    return;
                case "/api/live/stream":
                    LiveStream(response, request.QueryString["bot"]);

                    return;
                case "/api/debuggers":
                    Text(response, BotWebLive.Debuggers(), "application/json; charset=utf-8");

                    return;

                // The live picture (02.10.2026): the ground and what is built on it round a watched bot, cut on the loop when the
                // bot walks off the last square; the frame's pic.scene says which. See BotWebLivePicture.
                case "/api/live/scene":
                    LiveScene(response, request.QueryString["bot"]);

                    return;

                // The reach map (01.10.2026): its summary as the loop wrote it; the classes of a rectangle, a byte a tile or a
                // square, from the immutable snapshot; and one tile in words. See BotReachWeb.
                case "/api/reach":
                    Text(response, BotReachWeb.Json, "application/json; charset=utf-8");

                    return;
                case "/api/reach/tiles":
                    BotReachWeb.Tiles(request, response);

                    return;
                case "/api/reach/tile":
                    Text(response, BotReachWeb.Tile(request), "application/json; charset=utf-8");

                    return;

                // The stories (PLAN-life A3, 13.10.2026): one bot's chapters, or the index of stories when no bot is named; and the
                // island's Chronicle. Both are strings the loop wrote; asking for a story posts its name to the loop. See BotStory.
                case "/api/story":
                    Story(response, request.QueryString["bot"], request.QueryString["poll"] == "1");

                    return;
                case "/api/chronicle":
                    Text(
                        response,
                        BotStory.Running ? BotChronicle.Json : "{\"days\":[],\"note\":\"the stories are off: bot-story.json says Running is false\"}",
                        "application/json; charset=utf-8"
                    );

                    return;
            }

            // The client's own pictures, decoded on this thread from the client's files and kept. See BotWebArt.
            if (path.StartsWith("/api/art/", StringComparison.Ordinal) || path.StartsWith("/api/anim/", StringComparison.Ordinal))
            {
                BotWebArt.Serve(request, response, path);

                return;
            }

            if (path.StartsWith("/api/bot/", StringComparison.Ordinal))
            {
                var name = Uri.UnescapeDataString(path["/api/bot/".Length..]);
                var json = BotWebSnapshot.Bot(name);

                if (json == null)
                {
                    response.StatusCode = 404;
                    Text(response, "{\"error\":\"no such bot\"}", "application/json; charset=utf-8");
                }
                else
                {
                    Text(response, json, "application/json; charset=utf-8");
                }

                return;
            }

            // Anything else under the folder: a stylesheet or a script the page was split into.
            var safe = path.TrimStart('/').Replace('\\', '/');

            if (safe.Length > 0 && !safe.Contains("..", StringComparison.Ordinal))
            {
                var file = Path.Combine(FolderPath, safe);

                if (System.IO.File.Exists(file))
                {
                    File(response, file, ContentType(file));

                    return;
                }
            }

            response.StatusCode = 404;
            Text(response, "not found", "text/plain; charset=utf-8");
        }
        catch (Exception e)
        {
            Errors++;

            try
            {
                response.StatusCode = 500;
                Text(response, e.Message, "text/plain; charset=utf-8");
            }
            catch
            {
                // The client has gone.
            }
        }
    }

    private static void Text(HttpListenerResponse response, string text, string type)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? "");

        response.ContentType = type;
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }

    private static void File(HttpListenerResponse response, string file, string type)
    {
        if (file == null || !System.IO.File.Exists(file))
        {
            response.StatusCode = 404;
            Text(response, "not found", "text/plain; charset=utf-8");

            return;
        }

        var bytes = System.IO.File.ReadAllBytes(file);

        response.ContentType = type;
        response.ContentLength64 = bytes.Length;
        response.Headers["Cache-Control"] = "max-age=600";
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }

    private static string ContentType(string file) =>
        Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };

    /// <summary>The page, re-read when its file changes so that editing it needs no restart.</summary>
    private static string Page()
    {
        var file = Path.Combine(FolderPath, "index.html");

        if (!System.IO.File.Exists(file))
        {
            return "<!doctype html><meta charset=utf-8><title>BotWeb</title><body style=\"font-family:sans-serif;padding:2em\">"
                   + "<h1>BotWeb is listening</h1><p>No page yet: put <code>index.html</code> in <code>" + WebUtility.HtmlEncode(FolderPath)
                   + "</code>. The data is at <a href=\"/api/state\">/api/state</a>, <a href=\"/api/events\">/api/events</a>, "
                   + "<a href=\"/api/paths\">/api/paths</a>, <a href=\"/api/history\">/api/history</a>, <a href=\"/api/craft\">/api/craft</a>, <a href=\"/api/zones\">/api/zones</a>, "
                   + "<a href=\"/api/log\">/api/log</a>, <a href=\"/api/debuggers\">/api/debuggers</a>.</p></body>";
        }

        var stamp = System.IO.File.GetLastWriteTimeUtc(file);

        if (_page == null || stamp != _pageStamp)
        {
            _page = System.IO.File.ReadAllText(file);
            _pageStamp = stamp;
        }

        return _page;
    }

    private static string Events(HttpListenerRequest request)
    {
        var since = long.TryParse(request.QueryString["since"], out var s) ? s : -1;
        var limit = int.TryParse(request.QueryString["limit"], out var l) ? Math.Clamp(l, 1, 2000) : 200;
        var events = since < 0 ? BotEvents.Latest(limit, null) : BotEvents.Since(since, limit);
        var seq = BotEvents.Seq;

        var text = new StringBuilder(events.Count * 200 + 64);

        text.Append("{\"seq\":").Append(seq).Append(",\"events\":[");

        for (var i = 0; i < events.Count; i++)
        {
            if (i > 0)
            {
                text.Append(',');
            }

            text.Append(events[i].Json);
        }

        text.Append("]}");

        return text.ToString();
    }

    /// <summary>Server-sent events: every event as it is posted, and a comment every few seconds so a proxy keeps the line open.</summary>
    private static void Stream(HttpListenerResponse response)
    {
        var queue = new BlockingCollection<string>(new ConcurrentQueue<string>(), 1000);

        void Sink(BotEvent e)
        {
            if (!queue.TryAdd(e.Json))
            {
                // A reader that cannot keep up loses events rather than stalling the loop.
            }
        }

        BotEvents.Subscribe(Sink);
        Interlocked.Increment(ref _streams);

        try
        {
            response.ContentType = "text/event-stream; charset=utf-8";
            response.SendChunked = true;
            response.Headers["X-Accel-Buffering"] = "no";

            var output = response.OutputStream;

            Write(output, $": open {DateTime.Now:HH:mm:ss}\n\n");

            while (Running && !_stopping.IsCancellationRequested)
            {
                if (queue.TryTake(out var json, 5000))
                {
                    Write(output, "event: bot\ndata: " + json + "\n\n");
                }
                else
                {
                    Write(output, ": ping\n\n");
                }
            }
        }
        catch
        {
            // The browser closed the tab; nothing to say about it.
        }
        finally
        {
            BotEvents.Unsubscribe(Sink);
            Interlocked.Decrement(ref _streams);
            queue.Dispose();

            try
            {
                response.Close();
            }
            catch
            {
                // Already gone.
            }
        }
    }

    private static void Live(HttpListenerResponse response, string bot)
    {
        if (!BotWebLive.Enabled)
        {
            response.StatusCode = 503;
            Text(response, "{\"error\":\"the live window is off: bot-web.json says Live is false\"}", "application/json; charset=utf-8");

            return;
        }

        if (string.IsNullOrWhiteSpace(bot))
        {
            response.StatusCode = 400;
            Text(response, "{\"error\":\"name a bot: /api/live?bot=Name\"}", "application/json; charset=utf-8");

            return;
        }

        var (_, json) = BotWebLive.Ask(bot);

        Text(
            response,
            json ?? BotJson.Object(
                w =>
                {
                    w.WriteString("bot", bot.Trim());
                    w.WriteBoolean("pending", true);
                    w.WriteString("note", "watched from now; the first frame is built on the loop within a second");
                }
            ),
            "application/json; charset=utf-8"
        );
    }

    private static void Story(HttpListenerResponse response, string bot, bool poll)
    {
        if (!BotStory.Running)
        {
            response.StatusCode = 503;
            Text(response, "{\"error\":\"the stories are off: bot-story.json says Running is false\"}", "application/json; charset=utf-8");

            return;
        }

        if (BotStory.Sealed && !string.IsNullOrWhiteSpace(bot))
        {
            response.StatusCode = 503;
            Text(response, "{\"error\":\"the stories are sealed by do reset until the restart\"}", "application/json; charset=utf-8");

            return;
        }

        var json = BotStory.Story(bot, poll);

        if (json != null)
        {
            Text(response, json, "application/json; charset=utf-8");

            return;
        }

        // Not in the snapshot yet: the name has been posted to the loop, which builds the story within a tick if it is a bot's.
        response.StatusCode = 404;
        Text(
            response,
            BotJson.Object(
                w =>
                {
                    w.WriteString("bot", bot.Trim());
                    w.WriteBoolean("pending", true);
                    w.WriteString("note", "no story in the snapshot yet; if this is a bot, it is built on the loop within a few seconds");
                }
            ),
            "application/json; charset=utf-8"
        );
    }

    private static void LiveScene(HttpListenerResponse response, string bot)
    {
        if (!BotWebLive.Enabled || !BotWebLive.Picture)
        {
            response.StatusCode = 503;
            Text(response, "{\"error\":\"the live picture is off: bot-web.json says Live or LivePicture is false\"}", "application/json; charset=utf-8");

            return;
        }

        var scene = BotWebLive.SceneOf(bot);

        Text(
            response,
            scene ?? BotJson.Object(
                w =>
                {
                    w.WriteString("bot", bot?.Trim() ?? "");
                    w.WriteBoolean("pending", true);
                    w.WriteString("note", "no scene yet: ask /api/live?bot=Name first; the scene is cut on the loop within a second");
                }
            ),
            "application/json; charset=utf-8"
        );
    }

    /// <summary>The live frames of one bot as server-sent events: each new frame once, a comment every few seconds between.</summary>
    private static void LiveStream(HttpListenerResponse response, string bot)
    {
        if (!BotWebLive.Enabled || string.IsNullOrWhiteSpace(bot))
        {
            Live(response, bot);

            return;
        }

        Interlocked.Increment(ref _streams);

        try
        {
            response.ContentType = "text/event-stream; charset=utf-8";
            response.SendChunked = true;
            response.Headers["X-Accel-Buffering"] = "no";

            var output = response.OutputStream;
            var shown = -1L;
            var quiet = 0;

            Write(output, $": open {DateTime.Now:HH:mm:ss}\n\n");

            while (Running && !_stopping.IsCancellationRequested)
            {
                // Asking is what keeps the bot watched, so the stream asks four times a second and writes only a new frame.
                var (seq, json) = BotWebLive.Ask(bot);

                if (json != null && seq != shown)
                {
                    shown = seq;
                    quiet = 0;
                    Write(output, "event: frame\ndata: " + json + "\n\n");
                }
                else if (++quiet % 20 == 0)
                {
                    Write(output, ": ping\n\n");
                }

                _stopping.Token.WaitHandle.WaitOne(250);
            }
        }
        catch
        {
            // The browser closed the tab; nothing to say about it.
        }
        finally
        {
            Interlocked.Decrement(ref _streams);

            try
            {
                response.Close();
            }
            catch
            {
                // Already gone.
            }
        }
    }

    private static void Write(System.IO.Stream output, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        output.Write(bytes, 0, bytes.Length);
        output.Flush();
    }

    /// <summary>The tail of the current session log, filtered if asked.</summary>
    private static string Log(HttpListenerRequest request)
    {
        var n = int.TryParse(request.QueryString["n"], out var lines) ? Math.Clamp(lines, 1, 5000) : 300;
        var grep = request.QueryString["grep"];
        var file = NewestSessionLog();

        if (file == null)
        {
            return "no session log found";
        }

        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            // The last megabyte or so is enough for a few hundred lines of this log, and reading the whole
            // file every refresh would be reading a hundred megabytes for nothing by the end of a day.
            var take = Math.Min(stream.Length, Math.Max(256 * 1024, (long)n * 400));

            stream.Seek(stream.Length - take, SeekOrigin.Begin);

            using var reader = new StreamReader(stream, Encoding.UTF8);

            var all = reader.ReadToEnd().Split('\n');
            List<string> kept = new(n);

            for (var i = all.Length - 1; i >= 1 && kept.Count < n; i--)
            {
                var line = all[i].TrimEnd('\r');

                if (line.Length == 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(grep) || line.Contains(grep, StringComparison.OrdinalIgnoreCase))
                {
                    kept.Add(line);
                }
            }

            kept.Reverse();

            return string.Join('\n', kept);
        }
        catch (Exception e)
        {
            return "could not read the log: " + e.Message;
        }
    }

    public static string NewestSessionLog()
    {
        try
        {
            var folder = Path.GetFullPath(Path.Combine(Core.BaseDirectory, "..", "logs"));

            if (!Directory.Exists(folder))
            {
                return null;
            }

            string newest = null;
            var stamp = DateTime.MinValue;

            foreach (var name in Directory.GetFiles(folder, "session-*.log"))
            {
                var written = System.IO.File.GetLastWriteTimeUtc(name);

                if (written > stamp)
                {
                    stamp = written;
                    newest = name;
                }
            }

            return newest;
        }
        catch
        {
            return null;
        }
    }

    public static string Describe() =>
        Running
            ? $"the dashboard at {Url} has answered {Requests} requests ({Errors} badly) and holds {Streams} live streams"
            : "the dashboard is not listening";
}

import hashlib
from sys import argv
from enum import Enum

class Format(Enum):
    DEFAULT = 0
    RAW = 1
    RAW_UPPER = 2
    BINARY = 3
    BINARY_UPPER = 4
    ARRAY = 5

def printHelp():
    print('\nUsage: ' + argv[0] + ' [format-option] <username> <password>')
    print('''Options:
    --raw        Raw hash hex dump
    --raw-upper  Raw hash hex dump in uppercase
    --bin        Raw hash hex dump as a binary array
    --bin-upper  Raw hash hex dump in uppercase as a binary array
    --array      Raw hash hex dump as an array of character pairs\n''')


argc = len(argv)
printMode = Format.DEFAULT
usr = None
pwd = None
if(argc >= 2):
    if(argv[1] == '--help' or argv[1] == '-h' or argv[1] == '/?'):
        printHelp()
        exit(0)
    elif(argv[1] == '--raw'):
        printMode = Format.RAW
    elif(argv[1] == '--raw-upper'):
        printMode = Format.RAW_UPPER
    elif(argv[1] == '--bin'):
        printMode = Format.BINARY
    elif(argv[1] == '--bin-upper'):
        printMode = Format.BINARY_UPPER
    elif(argv[1] == '--array'):
        printMode = Format.ARRAY
if(argc == 4):
    usr = argv[2]
    pwd = argv[3]
elif(argc == 3):
    usr = argv[1]
    pwd = argv[2]
else:
    printHelp() ; exit(1)

if(usr == None):
    print("username: ", end='')
    usr = input()
if(pwd == None):
    print("password: ", end='')
    pwd = input()

hashobj = hashlib.sha1((usr + pwd).encode('utf-8'))
hash = hashobj.hexdigest()
if(printMode == Format.RAW):
    print(hash) ; exit(0)
elif(printMode == Format.BINARY):
    print(hash.encode('utf-8')) ; exit(0)
hash = hash.upper()
if(printMode == Format.RAW_UPPER):
    print(hash) ; exit(0)
elif(printMode == Format.BINARY_UPPER):
    print(hash.encode('utf-8')) ; exit(0)
hash = [hash[i:i+2] for i in range(0, len(hash), 2)]
if(printMode == Format.ARRAY):
    print(hash) ; exit(0)
hash = '-'.join(hash)
print(hash)
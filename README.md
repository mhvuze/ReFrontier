# ReFrontier
Tools for *packing, *crypting and editing various Monster Hunter Frontier Online files.

Repo made public as of 07/17/2019 on occasion of the game in question being shutdown in December 2019. Tool was intended for personal use only, so it might be a bit messy to foreign eyes. You're likely looking for ReFrontier itself and can safely ignore the other tools.

Huge thank you to enler for their help!

ReFrontier options:
```
-log: Write log file (required for repacking)
-nonRecursive: Do not unpack recursively (useful for modifying specific files in archives)
-pack: Repack directory (requires log file  - double check file extensions therein)
-decryptOnly: Decrypt ecd files without unpacking
-compress [type],[level]: Pack file with jpk [type] at compression [level] (example: -compress 3,10)
-encrypt: Encrypt input file with ecd algorithm
-close: Close window after finishing process
-cleanUp: Delete simple archives after unpacking
-ignoreJPK: Do not decompress JPK files
```

JPK type 4 compression test (tested on vanilla mhfdat.bin):
```
File compressed using type 4 (level 1): 9453891 bytes (64,26 % saved) in 0:04.48
File compressed using type 4 (level 2): 8589271 bytes (67,53 % saved) in 0:06.84
File compressed using type 4 (level 3): 8172392 bytes (69,11 % saved) in 0:08.80
File compressed using type 4 (level 4): 8029894 bytes (69,65 % saved) in 0:10.96
File compressed using type 4 (level 5): 7828552 bytes (70,41 % saved) in 0:12.83
File compressed using type 4 (level 6): 7641173 bytes (71,12 % saved) in 0:14.54
File compressed using type 4 (level 7): 7577461 bytes (71,36 % saved) in 0:16.67
File compressed using type 4 (level 8): 7504253 bytes (71,63 % saved) in 0:18.50
File compressed using type 4 (level 9): 7354422 bytes (72,20 % saved) in 0:20.00
File compressed using type 4 (level 10): 7320949 bytes (72,33 % saved) in 0:21.84
File compressed using type 4 (level 15): 7108825 bytes (73,13 % saved) in 0:30.62
File compressed using type 4 (level 20): 6673194 bytes (74,78 % saved) in 0:36.12
File compressed using type 4 (level 25): 6596009 bytes (75,07 % saved) in 0:43.72
File compressed using type 4 (level 30): 6525732 bytes (75,33 % saved) in 0:50.87
File compressed using type 4 (level 35): 6456423 bytes (75,59 % saved) in 0:58.75
File compressed using type 4 (level 40): 6290231 bytes (76,22 % saved) in 1:03.79
File compressed using type 4 (level 45): 6254283 bytes (76,36 % saved) in 1:10.46
File compressed using type 4 (level 50): 6216416 bytes (76,50 % saved) in 1:17.08
File compressed using type 4 (level 55): 6187694 bytes (76,61 % saved) in 1:23.79
File compressed using type 4 (level 60): 6155631 bytes (76,73 % saved) in 1:32.16
File compressed using type 4 (level 65): 6130964 bytes (76,82 % saved) in 1:38.39
File compressed using type 4 (level 70): 6109653 bytes (76,91 % saved) in 1:46.84
File compressed using type 4 (level 75): 6076931 bytes (77,03 % saved) in 1:52.92
File compressed using type 4 (level 80): 6052472 bytes (77,12 % saved) in 1:59.99
File compressed using type 4 (level 85): 6045761 bytes (77,15 % saved) in 2:01.77
File compressed using type 4 (level 90): 6045761 bytes (77,15 % saved) in 2:02.25
File compressed using type 4 (level 95): 6045761 bytes (77,15 % saved) in 2:04.26
File compressed using type 4 (level 100): 6045761 bytes (77,15 % saved) in 2:05.03
Vanilla file from COG with type 4       : 5363764 bytes (79,72 % saved)
```

Some more useful tools and projects:
- [Blender plugin for 3D models](https://github.com/AsteriskAmpersand/Monster-Hunter-Frontier-Importer)
- [P-Server project](https://github.com/Andoryuuta/Erupe)
- [Mira's 010 templates](https://github.com/CrimsonMiralis/MHFZ-Templates)
- [Fist's 010 templates](https://github.com/SirFist/fz-010-scripts)

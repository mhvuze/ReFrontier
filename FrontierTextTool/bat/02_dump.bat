@echo off
move /Y F:\MHFrontier\mhfdat\MHFUP_00.DAT src

C:\Users\corne\Source\Repos\ReFrontier\ReFrontier\bin\Debug\ReFrontier.exe mhfdat.bin -log -close
frontiertexttool dump mhfdat.bin 3040 3328506 -close
move /Y mhfdat.bin.meta output
move /Y mhfdat.bin src

C:\Users\corne\Source\Repos\ReFrontier\ReFrontier\bin\Debug\ReFrontier.exe mhfpac.bin -log -close
frontiertexttool dump mhfpac.bin 4416 1278736 -close
move /Y mhfpac.bin.meta output
move /Y mhfpac.bin src

pause
@echo off
title Ermay Muhasebe - Mobil Test Ekrani (Tunel)
echo ===================================================
echo  Guvenlik duvari asiliyor, tunel baslatiliyor...
echo  (Birazdan bu ekranda kocaman bir QR kod belirecek)
echo ===================================================
echo.
call npx expo start --tunnel
pause

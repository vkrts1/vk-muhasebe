; Script generated for VK Ön Muhasebe
#define MyAppName "VK"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "VK"
#define MyAppURL "https://vk.com"
#define MyAppExeName "VK.exe"

[Setup]
AppId={{C8E11D92-4820-43A5-9DAE-9BEB6B2BBA31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Publish_Output\Installer
OutputBaseFilename=VK_Setup_v1.0.0
SetupIconFile=ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia.Desktop\Assets\app.ico
UninstallDisplayIcon={app}\Assets\app.ico
UninstallDisplayName={#MyAppName} Programını Kaldır
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "cleandatabase"; Description: "Temiz Kurulum: Mevcut yerel veritabanını sıfırla (Bütün eski test verilerini temizle)"; GroupDescription: "Veritabanı Seçenekleri:"; Flags: unchecked

[Files]
Source: "Publish_Output\Desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"
Name: "{autoprograms}\{#MyAppName} Kaldır (Uninstall)"; Filename: "{uninstallexe}"; IconFilename: "{app}\Assets\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  InfoPage: TOutputMsgWizardPage;
  FirebasePage: TInputQueryWizardPage;
  TelegramPage: TInputQueryWizardPage;
  EmailSmtpPage: TInputQueryWizardPage;
  User1Page: TInputQueryWizardPage;
  User2Page: TInputQueryWizardPage;

procedure InitializeWizard;
var
  GuideText: String;
begin
  // 1. Detaylı ve Adım Adım Bilgilendirme / API Rehber Ekranı
  GuideText := 
    'VK Ön Muhasebe kurulumuna hoş geldiniz!' + #13#10 + #13#10 +
    '📌 1. GOOGLE İLE TEK TIKLA GİRİŞ (GOOGLE OAUTH CLIENT ID) NASIL ALINIR?' + #13#10 +
    '   - https://console.firebase.google.com > Projeniz > "Authentication" > "Sign-in method" > "Google" seçeneğini etkinleştirin.' + #13#10 +
    '   - https://console.cloud.google.com/apis/credentials sayfasına gidin (veya Firebase Proje Ayarları).' + #13#10 +
    '   - "OAuth 2.0 İstemci Kimlikleri (OAuth 2.0 Client IDs)" altındaki "Web Client" satırındaki İstemci Kimliğini (Client ID) kopyalayın.' + #13#10 +
    '   - Örnek format: 123456789-abcdef.apps.googleusercontent.com' + #13#10 +
    '   - Bu sayede masaüstü ve mobilde Google ile tek tıkla oturum açabilirsiniz.' + #13#10 + #13#10 +
    '📌 2. GMAIL İLE ÜCRETSİZ E-POSTA VE ŞİFRE SIFIRLAMA NASIL YAPILIR?' + #13#10 +
    '   - https://myaccount.google.com/security adresine gidin ve 2 Adımlı Doğrulama''yı açın.' + #13#10 +
    '   - Arama çubuğuna "Uygulama Şifreleri" (App Passwords) yazın.' + #13#10 +
    '   - Uygulama adı olarak "VK" yazıp 16 haneli şifreyi kopyalayın.' + #13#10 +
    '   - E-posta alanına Gmail adresinizi, şifre alanına ise bu 16 haneli şifreyi yazın.' + #13#10 + #13#10 +
    '📌 3. GOOGLE CLOUD & PDF İÇİN WEB API KEY NASIL ALINIR?' + #13#10 +
    '   - https://console.firebase.google.com > Proje Ayarları > "Genel" sekmesindeki "Web API Anahtarı"nı kopyalayın.' + #13#10 + #13#10 +
    '📌 4. FIREBASE REALTIME DATABASE BİLGİLERİ NASIL ALINIR?' + #13#10 +
    '   - Database URL: "Build" > "Realtime Database" alanındaki https://projeniz-rtdb.firebaseio.com adresini kopyalayın.' + #13#10 +
    '   - Secret Key: Proje Ayarları > "Hizmet Hesapları" > "Veritabanı Gizli Anahtarları" sekmesinden anahtarı kopyalayın.' + #13#10 + #13#10 +
    '📌 5. TELEGRAM BOT İLE ŞİFRE SIFIRLAMA / OTP BİLGİLERİ NASIL ALINIR?' + #13#10 +
    '   - Bot Token: Telegram uygulamasında "@BotFather" ile bot oluşturup Token''ı kopyalayın.' + #13#10 +
    '   - Chat ID: Telegramda "@userinfobot" ile sayısal Chat ID numaranızı öğrenin.' + #13#10 + #13#10 +
    '📌 6. FABRİKA AYARLARI VE SIFIRLAMA GÜVENLİK ŞİFRESİ:' + #13#10 +
    '   - Standart "ERMAY2025" şifresi silinecek, belirleyeceğiniz şifre kalıcı güvenlik şifreniz olacaktır.' + #13#10 + #13#10 +
    'Bilgileri henüz temin etmediyseniz alanları boş bırakarak kuruluma devam edebilirsiniz. Program yerel (offline) olarak hemen açılacaktır.';

  InfoPage := CreateOutputMsgPage(wpSelectDir,
    'Kurulum, Google Giriş, Gmail, Firebase ve Telegram Rehberi',
    'Tüm Google OAuth, API ve güvenlik bilgilerinizi nereden alacağınızı adım adım inceleyin:',
    GuideText);

  // 2. Firebase & Google API / OAuth Yapılandırma Sayfası
  FirebasePage := CreateInputQueryPage(InfoPage.ID,
    'Bulut, Mobil Senkronizasyon & Google Giriş Yapılandırması',
    'Mobil-Masaüstü veri aktarımı, PDF servisleri ve Google ile Giriş için bilgilerinizi giriniz.',
    'Bilgiler bilgisayarınızda şifreli olarak saklanacaktır:');

  FirebasePage.Add('Firebase Realtime Database URL (örn: https://projeniz.firebaseio.com):', False);
  FirebasePage.Add('Firebase Database Secret / Gizli Anahtar:', True);
  FirebasePage.Add('Google Web API Key (PDF & Bulut Servisleri):', False);
  FirebasePage.Add('Google OAuth Client ID (Google ile Giriş için):', False);
  FirebasePage.Add('Google OAuth Client Secret (İsteğe bağlı):', True);

  // 3. Gmail / SMTP E-Posta Yapılandırma Sayfası
  EmailSmtpPage := CreateInputQueryPage(FirebasePage.ID,
    'Gmail / SMTP E-Posta Bildirim & Şifre Sıfırlama',
    'Şifre unutulduğunda e-posta ile kurtarma kodu göndermek için Gmail SMTP bilgilerinizi giriniz.',
    'İsteğe bağlıdır (Rehberdeki 2. maddede anlatılan Gmail Uygulama Şifresini giriniz):');

  EmailSmtpPage.Add('Gönderici Gmail / E-Posta Adresi (örn: muhasebe@gmail.com):', False);
  EmailSmtpPage.Add('Gmail 16 Haneli Uygulama Şifresi (App Password):', True);

  // 4. Telegram Şifre Kurtarma & Bot Yapılandırma Sayfası
  TelegramPage := CreateInputQueryPage(EmailSmtpPage.ID,
    'Telegram ile Güvenli Şifre Kurtarma & OTP Yapılandırması',
    'Şifre unutulduğunda Telegram üzerinden anlık OTP doğrulama kodu almak için bot bilgilerinizi giriniz.',
    'İsteğe bağlıdır, boş bırakabilirsiniz:');

  TelegramPage.Add('Telegram Bot Token (örn: 123456789:ABCdefGhIJKlmNoPQRstuVWXyz):', True);
  TelegramPage.Add('Telegram Yönetici Chat ID (örn: 987654321):', False);

  // 5. Kullanıcı 1 (Ana Yönetici) ve Sistem Sıfırlama Şifresi Belirleme Sayfası
  User1Page := CreateInputQueryPage(TelegramPage.ID,
    'Kullanıcı Hesabı 1 (Ana Yönetici) & Sıfırlama Şifresi',
    'Sisteme giriş yapacak yönetici hesabı ve fabrika ayarlarına sıfırlama onay şifresini belirleyin.',
    'Bu hesap oluşturulduğunda varsayılan admin/123 girişi tamamen engellenecektir:');

  User1Page.Add('1. Kullanıcı Adı (örn: patron, muhasebe):', False);
  User1Page.Add('1. Kullanıcı Şifresi:', True);
  User1Page.Add('1. Kullanıcı E-Posta Adresi (Şifre kurtarma için):', False);
  User1Page.Add('Fabrika Ayarları & Sıfırlama Onay Şifresi:', True);
  User1Page.Values[3] := 'VK2026';

  // 6. Kullanıcı 2 (Ek Personel / Yedek Kullanıcı) Belirleme Sayfası
  User2Page := CreateInputQueryPage(User1Page.ID,
    'Kullanıcı Hesabı 2 (Opsiyonel İkinci Kullanıcı)',
    'Programa erişebilecek 2. Kullanıcıyı tanımlayabilirsiniz (İstemiyorsanız boş bırakabilirsiniz).',
    'Ek kullanıcı bilgileri:');

  User2Page.Add('2. Kullanıcı Adı (İsteğe bağlı):', False);
  User2Page.Add('2. Kullanıcı Şifresi:', True);
  User2Page.Add('2. Kullanıcı E-Posta Adresi:', False);
end;

procedure DeleteFilesByPattern(const Dir, Pattern: String);
var
  FindRec: TFindRec;
begin
  if FindFirst(Dir + '\' + Pattern, FindRec) then
  begin
    try
      repeat
        DeleteFile(Dir + '\' + FindRec.Name);
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  UrlVal, SecretVal, GoogleApiKeyVal, GoogleClientIdVal, GoogleClientSecretVal, SmtpEmailVal, SmtpPassVal, BotTokenVal, ChatIdVal, User1Name, User1Pass, User1Email, User2Name, User2Pass, User2Email, FactoryResetPass, AppDataDir, ConfigPath, UserConfigPath, JsonContent, UserJsonContent, AuthQuery, FirebasePostScript: String;
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    UrlVal := Trim(FirebasePage.Values[0]);
    SecretVal := Trim(FirebasePage.Values[1]);
    GoogleApiKeyVal := Trim(FirebasePage.Values[2]);
    GoogleClientIdVal := Trim(FirebasePage.Values[3]);
    GoogleClientSecretVal := Trim(FirebasePage.Values[4]);

    SmtpEmailVal := Trim(EmailSmtpPage.Values[0]);
    SmtpPassVal := Trim(EmailSmtpPage.Values[1]);

    BotTokenVal := Trim(TelegramPage.Values[0]);
    ChatIdVal := Trim(TelegramPage.Values[1]);
    
    User1Name := Trim(User1Page.Values[0]);
    User1Pass := Trim(User1Page.Values[1]);
    User1Email := Trim(User1Page.Values[2]);

    User2Name := Trim(User2Page.Values[0]);
    User2Pass := Trim(User2Page.Values[1]);
    User2Email := Trim(User2Page.Values[2]);

    FactoryResetPass := Trim(User1Page.Values[3]);
    if FactoryResetPass = '' then FactoryResetPass := 'VK2026';

    // Eğer 1. kullanıcı boş bırakıldıysa:
    // Eğer Firebase URL girilmişse (Bulut senkronizasyonu aktif), kullanıcı boş bırakılır böylece uygulama açılışta buluttan hesapları çeker.
    // Eğer Firebase URL de girilmemişse (tamamen yerel kurulum), varsayılan 'admin' / '123' atanır.
    if User1Name = '' then 
    begin
      if (UrlVal = '') and (GoogleApiKeyVal = '') then
      begin
        User1Name := 'admin';
        User1Pass := '123';
      end;
    end
    else if User1Pass = '' then
    begin
      User1Pass := '123';
    end;

    AppDataDir := ExpandConstant('{localappdata}');
    
    // 0. Temiz Kurulum: Veritabanı Sıfırla
    if WizardIsTaskSelected('cleandatabase') then
    begin
      DeleteFilesByPattern(AppDataDir + '\ErmayMuhasebe', '*.db');
      DeleteFilesByPattern(AppDataDir + '\ErmayMuhasebe', '*.db-wal');
      DeleteFilesByPattern(AppDataDir + '\ErmayMuhasebe', '*.db-shm');
      DeleteFilesByPattern(AppDataDir + '\ErmayMuhasebe', '*.db3');
      DeleteFilesByPattern(AppDataDir + '\ErmayMuhasebe', '*.db3-wal');
      DeleteFilesByPattern(AppDataDir + '\ErmayMuhasebe', '*.db3-shm');
      DeleteFile(AppDataDir + '\ErmayMuhasebe\login_settings.txt');
    end;
    
    // 1. Bulut Config Kaydet
    if (UrlVal <> '') or (GoogleApiKeyVal <> '') or (GoogleClientIdVal <> '') then
    begin
      ConfigPath := AppDataDir + '\ermay_cloud_config.json';
      JsonContent := '{"BaseUrl":"' + UrlVal + '","AuthSecret":"' + SecretVal + '","GoogleApiKey":"' + GoogleApiKeyVal + '","GoogleClientId":"' + GoogleClientIdVal + '","GoogleClientSecret":"' + GoogleClientSecretVal + '","IsActive":true,"IsAutoSyncEnabled":true}';
      SaveStringToFile(ConfigPath, JsonContent, False);
    end;

    // 2. Kullanıcı/Şifre, SMTP, Telegram, Google Client ID ve Fabrika Sıfırlama Şifresi Yapılandırma Dosyası Kaydet
    ForceDirectories(AppDataDir + '\ErmayMuhasebe');
    UserConfigPath := AppDataDir + '\ErmayMuhasebe\setup_initial_user.json';
    
    if User1Name <> '' then
    begin
      UserJsonContent := '{"Users":[' +
        '{"Username":"' + User1Name + '","Password":"' + User1Pass + '","Email":"' + User1Email + '"}' ;
      
      if (User2Name <> '') and (User2Pass <> '') then
      begin
        UserJsonContent := UserJsonContent + ',{"Username":"' + User2Name + '","Password":"' + User2Pass + '","Email":"' + User2Email + '"}';
      end;
      
      UserJsonContent := UserJsonContent + '],' ;
    end
    else
    begin
      UserJsonContent := '{"Users":[],' ;
    end;
    
    UserJsonContent := UserJsonContent +
      '"FactoryResetPassword":"' + FactoryResetPass + '",' +
      '"GoogleClientId":"' + GoogleClientIdVal + '",' +
      '"GoogleClientSecret":"' + GoogleClientSecretVal + '",' +
      '"SmtpEmail":"' + SmtpEmailVal + '",' +
      '"SmtpPass":"' + SmtpPassVal + '",' +
      '"TelegramBotToken":"' + BotTokenVal + '",' +
      '"TelegramChatId":"' + ChatIdVal + '"}';
      
    SaveStringToFile(UserConfigPath, UserJsonContent, False);

    // 3. Eger kullanici adi ve Firebase URL girilmisse, Firebase'e de aninda gonder
    if (UrlVal <> '') and (User1Name <> '') and (User1Pass <> '') then
    begin
      try
        begin
          AuthQuery := '';
          if SecretVal <> '' then
            AuthQuery := '?auth=' + SecretVal;

          FirebasePostScript :=
            '$sb = New-Object byte[] 16; ' +
            '[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($sb); ' +
            '$salt = [Convert]::ToBase64String($sb); ' +
            '$sha = [System.Security.Cryptography.SHA256]::Create(); ' +
            '$h = [Convert]::ToBase64String($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes(''' + User1Pass + ''' + $salt))); ' +
            '$body = ''{\""Id\"":1,\""Username\"":\""' + User1Name + '\"",\""Password\"":\""'' + $h + ''\"",\""PasswordSalt\"":\""'' + $salt + ''\"",\""Role\"":\""Admin\"",\""CreatedAt\"":\""'' + (Get-Date -Format s) + ''\""}''; ' +
            '$u = ''' + UrlVal + '/users/1.json' + AuthQuery + '''; ' +
            'try { Invoke-RestMethod -Uri $u -Method Put -Body $body -ContentType ''application/json'' } catch {}';

          Exec('powershell.exe', '-WindowStyle Hidden -ExecutionPolicy Bypass -Command "' + FirebasePostScript + '"', '', SW_HIDE, ewNoWait, ResultCode);
        end;
      except
      end;
    end;
  end;
end;

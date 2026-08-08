# PesaScope — Android Release & Play Store Deploy Guide

## 1. Generate the upload keystore

Run once, keep the resulting file forever:

```bash
keytool -genkeypair -v -keystore pesascope-upload.keystore -alias pesascope-upload -keyalg RSA -keysize 2048 -validity 10000
```

- `-validity 10000` = ~27.4 years (until ~May 2054). Google requires the upload key certificate to stay valid at least until **22 Oct 2033**, so this comfortably covers that.
- You'll be prompted for a **keystore password** and a **key password** (often the same value) plus your name/org/country details.
- **Keep the keystore file and both passwords safe.** Losing them blocks you from publishing updates under the same app.
- Add the keystore file to `.gitignore` — never commit it to source control.

## 2. Set signing passwords as environment variables

The publish command below uses `env:` prefixes so passwords never appear in shell history or CI logs. Set these in your terminal session before publishing.

**Linux/macOS (bash/zsh):**
```bash
export KEY_PASS="your-key-password"
export STORE_PASS="your-store-password"
```

**Windows PowerShell:**
```powershell
$env:KEY_PASS = "your-key-password"
$env:STORE_PASS = "your-store-password"
```

Note: these only persist for the current terminal session. Re-set them each time, or source them from a local `.env` file (kept out of git) — don't put secrets in a shared shell profile.

## 3. Publish a signed release build

**Important:** `AndroidSigningKeyStore` is resolved relative to the **project file's directory** (`PesaScope.App\`), not your solution root or wherever you run the command from. Put the keystore inside `PesaScope.App\` — if it's currently at the solution root, move it:

```powershell
move pesascope-upload.keystore PesaScope.App\pesascope-upload.keystore
```

Also target the **App project explicitly**, not the whole solution — `PesaScope.Core` and `PesaScope.Tests` don't target `net10.0-android` and will fail with `NETSDK1005` if you try to publish the solution as a whole.

**PowerShell (Windows):**
```powershell
dotnet publish PesaScope.App\PesaScope.App.csproj -f net10.0-android -c Release `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore=pesascope-upload.keystore `
  -p:AndroidSigningKeyAlias=pesascope-upload `
  -p:AndroidSigningKeyPass=env:KEY_PASS `
  -p:AndroidSigningStorePass=env:STORE_PASS
```

**bash/zsh (Linux/macOS):**
```bash
dotnet publish PesaScope.App/PesaScope.App.csproj -f net10.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=pesascope-upload.keystore \
  -p:AndroidSigningKeyAlias=pesascope-upload \
  -p:AndroidSigningKeyPass=env:KEY_PASS \
  -p:AndroidSigningStorePass=env:STORE_PASS
```

| Flag | Meaning |
|---|---|
| `PesaScope.App\PesaScope.App.csproj` | Publish the app project directly, not the solution |
| `-f net10.0-android` | Target framework — matches the `TargetFrameworks` in the `.csproj` |
| `-c Release` | Release configuration |
| `AndroidKeyStore=true` | Tells MSBuild to sign the build |
| `AndroidSigningKeyStore` | Keystore filename from step 1, must live in `PesaScope.App\` |
| `AndroidSigningKeyAlias` | Alias chosen in step 1 (`pesascope-upload`) |
| `AndroidSigningKeyPass` | Key password, read from `env:KEY_PASS` |
| `AndroidSigningStorePass` | Store password, read from `env:STORE_PASS` |

This produces a signed `.aab` (and `.apk`) ready to upload.

**Double-check `.gitignore`** has a line excluding `*.keystore` so this file never ends up in git history.

### Where to find the output files to upload

`dotnet publish` writes the signed build artifacts under the App project's `bin` folder:

```
PesaScope.App\bin\Release\net10.0-android\
```

Inside that folder look for:
- **`*-Signed.aab`** — the Android App Bundle. This is what you upload to Play Console (internal testing track, and later production).
- **`*-Signed.apk`** — a signed APK, useful for sideloading/manual testing on a device but not what Play Console wants for the bundle upload.

The exact filename is usually `com.bkokumu.pesascope-Signed.aab` (based on your `ApplicationId`), but confirm by listing the folder after publish completes — MAUI sometimes appends the version or leaves off the `-Signed` suffix depending on SDK version.

## 4. Upload to Play Console

- Upload the signed `.aab` to the **Internal testing** track.
- Play Console will auto-populate the **upload key certificate fingerprints** (SHA-1/SHA-256) after this first upload.
- Google re-signs the app with its own **app signing key** on their end (Play App Signing) — you don't manage that key directly.

## 5. When you'd actually need the SHA-1 / SHA-256 fingerprints

Not needed for basic publishing. Only relevant if:

- **Firebase / Google Sign-In / Maps SDK** — register the **SHA-1** (and SHA-256 for some newer APIs) of the *app signing key* in Firebase/Google Cloud console.
- **Digital Asset Links** (Android App Links / TWA to a website domain) — paste the **SHA-256** fingerprint into `assetlinks.json` hosted at your domain's `/.well-known/` path.

Since PesaScope is a local on-device app with no associated web domain, both can be skipped for now.

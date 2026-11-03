# Code signing and release preparation

The public signature has not yet taken place. There is no signing certificate available in this installation and no vendor account configured in the project.

The build process accepts Authenticode signing through a certificate with a private key available in the Windows `CurrentUser/My` repository and the Windows SDK SignTool. First validate the prerequisites, then sign the overlay before embedding it in the Companion, and finally sign the Companion. Each signature is verified, including RFC3161 timestamp. Executables cannot be modified once signed.

```powershell
.\build.ps1 -Test -SigningThumbprint <certificate-thumbprint> -TimestampUrl <rfc3161-timestamp-url> -SignToolPath <path-to-signtool.exe>
```

Without these parameters, the process continues to generate an unsigned test build and indicates this state. The `.zip.sha256` file allows you to check the integrity of the ZIP; does not replace the publisher's signature. Do not store private certificates, keys, credentials or passwords in the repository.

The GPLv3 license was chosen. An application to the SignPath Foundation for open-source code signing may be considered. Acceptance requires design review, verifiable code and compilation, and approval of each signature. This application and the integration of the service have not yet been carried out. The local certificate script above does not configure the SignPath.

The original code license is GPLv3 (`GPL-3.0-only`). Published builds and their release status are listed on the GitHub releases page.

References:

- [SignTool, Microsoft](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool)
- [Azure Artifact Signing eligibility](https://learn.microsoft.com/en-gb/azure/artifact-signing/quickstart)
- [SignPath Foundation Conditions](https://signpath.org/terms.html)

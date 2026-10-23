# DS4Windows

> **This project has moved** to **https://github.com/gwin7ok/DS4Windows** (formerly `gwin7ok/DS4Windows-Vader4Pro`).
> Please open new issues and pull requests there, and download new releases from there.
> - Version 4.0.1 or earlier: update to 4.0.2 from the in-app updater. It will move you to the new repository.
> - Version 4.0.2 or later: the app checks the new repository for updates.
> - Back up your profile folder (`Profiles` and `Actions.xml`) before updating.
>
> This repository is kept only so that older versions can find 4.0.2, and will be archived.
>
> **このプロジェクトは移転しました。** 今後は **https://github.com/gwin7ok/DS4Windows**（旧 `gwin7ok/DS4Windows-Vader4Pro`）で開発・配布します。
> 新しい Issue と PR は移転先へ、新しいリリースも移転先から入手してください。
> - 4.0.1 以前: アプリの更新機能で 4.0.2 に更新すると、移転先に移行されます。
> - 4.0.2 以降: 移転先で更新を確認します。
> - 更新前に、プロファイルのフォルダ（`Profiles` と `Actions.xml`）をバックアップしてください。
>
> このリポジトリは、古いバージョンが 4.0.2 を見つけるためだけに残してあり、アーカイブします。

Like those other DS4 tools, but sexier.

DS4Windows is an extract anywhere program that allows you to get the best
DualShock 4 experience on your PC. By emulating an Xbox 360 controller, many
more games are accessible. Other input controllers are also supported including the
DualSense, Switch Pro, and JoyCon controllers (**first party hardware only**).

This project is a fork of the work of Jays2Kings and Ryochan7. It adds various new features like switch 
[debouncing](https://www.ganssle.com/debouncing.pdf), a tool that helps to fix stick drift and pitch and roll simulation
for DS3 based on accelerometer value (which is a work of [sunnyqeen](https://github.com/sunnyqeen)).

![DS4Windows Preview](https://raw.githubusercontent.com/Ryochan7/DS4Windows/jay/ds4winwpf_screen_20200412.png)

## About this fork

I've made this fork because some of the buttons on my controller started bouncing. Normally I would just add a
feature that would fix my problem, make a pull request to the original repo and forget about the project. 
The issue here is that Ryochan7 stopped maintaining the original project, so I decided to make slight 
modifications to the code that detects if the installed version is up-to-date, so it now pulls version info from my 
repo. This way if you install my version, you don't get the annoying popup saying your version is outdated. If there 
are any feature requests, I'm more than happy to at least look at them and assess whether I could add them.

## License

DS4Windows is licensed under the terms of the GNU General Public License version 3.
You can find a copy of the terms and conditions of that license at
[https://www.gnu.org/licenses/gpl-3.0.txt](https://www.gnu.org/licenses/gpl-3.0.txt). The license is also
available in this source code from the COPYING file.

## Downloads

- **[Main builds of DS4Windows](https://github.com/gwin7ok/DS4Windows/releases)**

## Install

You can install DS4Windows by downloading it from [releases](https://github.com/gwin7ok/DS4Windows/releases) and place it to your preferred place.

Alternatively, you can install [`ds4windows`](https://scoop.sh/#/apps?q=ds4windows&o=true&id=c8b519fcb06da6bb014569fd0a07521839ec5425) via [Scoop](https://scoop.sh/).

Alternatively, you can download [`ds4w.bat`](https://raw.githubusercontent.com/gwin7ok/DS4Windows/refs/heads/master/ds4w.bat) file and execute it. It will open a window that downloads and places the program in `%LOCALAPPDATA%\DS4Windows` and creates a desktop shortcut to the executable.

## Requirements

- Windows 10 or newer (Thanks Microsoft)
- Microsoft .NET 8.0 Desktop Runtime (x64). Download the x64 runtime from Microsoft's site.
- Visual C++ 2015-2022 Redistributable (x64). Download the x64 redistributable from Microsoft.
- [ViGEmBus](https://vigem.org/) driver (DS4Windows will install it for you)
- **Sony** DualShock 4 or other supported controller
- Connection method:
  - Micro USB cable
  - [Sony Wireless Adapter](https://www.amazon.com/gp/product/B01KYVLKG2)
  - Bluetooth 4.0 (via an
  [adapter like this](https://www.newegg.com/Product/Product.aspx?Item=N82E16833166126)
  or built in pc). Only use of Microsoft BT stack is supported. CSR BT stack is
  confirmed to not work with the DS4 even though some CSR adapters work fine
  using Microsoft BT stack. Toshiba's adapters currently do not work.
  *Disabling 'Enable output data' in the controller profile settings might help with latency issues, but will disable lightbar and rumble support.*
- Disable **PlayStation Configuration Support** and
**Xbox Configuration Support** options in Steam

## Modifications in This Fork (gwin7ok)
This fork's executable is configured to always run with administrator privileges via its application manifest (the manifest requests `requireAdministrator`).

このフォークでは実行ファイルのアプリケーションマニフェストにより、常に管理者権限での起動を前提とする設計に変更されています。インストール、ログ出力、および設定保存は管理者コンテキストを前提とします。

The detailed history of changes for this fork has been moved to `CHANGELOG.md`.

### Version 4.0.0
4.0.0 is a major release: the internal architecture was rebuilt on dependency injection, and the Controls and Special Actions tabs now share one assignment editor (Switch Mode for every assignment, SA Extras, a Duplicate button, and a "Reset all to Default" button). The "360 Steering Wheel" emulation and the XboxGameDVR special action were removed.
**Please back up your profile folder before updating**, because profiles saved by 4.0.0 may not be readable by 3.x. See [CHANGELOG.md](./CHANGELOG.md) for details.

4.0.0 では内部構造を DI（依存性注入）ベースに全面的に作り直し、Controls タブと Special Actions タブの割り当て画面を統一しました。「360 Steering Wheel」と XboxGameDVR は廃止されました。**更新前にプロファイルのバックアップを取ってください**（4.0.0 で保存したプロファイルは 3.x で読めない場合があります）。

Please see [CHANGELOG.md](./CHANGELOG.md) for versioned release notes and details. Visit the Releases page for the latest build.

_This README retains a short pointer — full changelog entries are maintained in `CHANGELOG.md`._

## Installation Guide / 導入ガイド

- See the installation and setup guide: [Installation Guide (Japanese)](導入ガイド.md)
- インストール手順と初回セットアップは次のガイドを参照してください: [導入ガイド](導入ガイド.md)
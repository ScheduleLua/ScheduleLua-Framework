# ScheduleLua

<img src="logo.png" alt="ScheduleLua Logo" width="333">

[![Version](https://img.shields.io/badge/version-0.1.6-blue.svg)](https://thunderstore.io/c/schedule-i/p/ScheduleLua/ScheduleLua/versions/)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-beta-orange.svg)](https://schedulelua.github.io/ScheduleLua-Docs/guide/development-status.html)
[![MelonLoader](https://img.shields.io/badge/requires-MelonLoader-red.svg)](https://melonwiki.xyz/)
[![Lua](https://img.shields.io/badge/language-Lua-blue.svg)](https://www.lua.org/)
[![C#](https://img.shields.io/badge/language-C%23-darkgreen.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Contributions](https://img.shields.io/badge/contributions-welcome-brightgreen.svg)](https://schedulelua.github.io/ScheduleLua-Docs/guide/contributing.html)

A Lua modding framework for Schedule I that aims to expose the game's functionality to Lua scripts, enabling custom gameplay mechanics, automation, and new features. ScheduleLua is currently in beta development, and the only features that are known to be working properly are the ones in the example scripts. I and or other contributors cannot guarantee that everything will work or be available, especially after the game updates.

## Table of Contents

- [ScheduleLua](#schedulelua)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Features](#features)
  - [Installation](#installation)
  - [Getting Started](#getting-started)
  - [Configuration](#configuration)
  - [Contributing](#contributing)
    - [Getting Started](#getting-started-1)
    - [Making Changes](#making-changes)
    - [Submitting Your Contribution](#submitting-your-contribution)
    - [Contribution Guidelines](#contribution-guidelines)
  - [License](#license)
  - [Acknowledgments](#acknowledgments)

## Overview

ScheduleLua is a MelonLoader mod that integrates the MoonSharp Lua interpreter with Schedule I, providing an easy to learn, flexible scripting environment. The framework exposes core game systems through a Lua API, allowing modders to create custom gameplay experiences without direct C# coding.

## Features

- **Robust Lua Environment**: Built on MoonSharp for .NET integration
- **Hot Reloading**: Edit scripts while the game is running for rapid development
- **Event System**: Subscribe to game events like day changes, player status updates, etc.
- **ScheduleOne API**: Access to player, NPCs, and more
- **Error Handling**: Detailed error reporting and script isolation
- **Mod Configuration**: Configurable settings via MelonPreferences

## Installation

[![MelonLoader Required](https://img.shields.io/badge/MelonLoader-Required-red)](https://melonwiki.xyz/#/?id=automated-installation)
[![Schedule I](https://img.shields.io/badge/Game-Schedule_I-blue)](https://store.steampowered.com/)
[![Latest Release](https://img.shields.io/badge/Latest_Release-Thunderstore-brightgreen)](https://thunderstore.io/)

1. Install [MelonLoader](https://melonwiki.xyz/#/?id=automated-installation) for Schedule I
2. Download the latest ScheduleLua release zip from [Thunderstore](https://thunderstore.io/)
3. Extract the zip file and drag the `Mods` and `UserLibs` folders into your Schedule I game directory
4. Launch the game

## Getting Started

For a comprehensive guide on getting started with ScheduleLua, visit our [online documentation](https://schedulelua.github.io/ScheduleLua-Docs/guide/getting-started.html).

## Configuration

Edit settings in `UserData/MelonPreferences.cfg`:

```
[ScheduleLua]
EnableHotReload = true
LogScriptErrors = true
```

## Contributing

[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/ScheduleLua/ScheduleLua-Framework/pulls)
[![Issues](https://img.shields.io/badge/Issues-welcome-blue.svg)](https://github.com/ScheduleLua/ScheduleLua-Framework/issues)
[![Contributors](https://img.shields.io/badge/Contributors-help_wanted-orange.svg)](https://github.com/ScheduleLua/ScheduleLua-Framework/graphs/contributors)

We welcome contributions to ScheduleLua! This guide will walk you through the process of contributing to the project.

### Getting Started

1. **Fork the Repository**
   - Visit [ScheduleLua GitHub repository](https://github.com/ScheduleLua/ScheduleLua-Framework)
   - Click the "Fork" button in the top right corner
   - This creates a copy of the repository in your GitHub account

2. **Clone Your Fork**
   ```bash
   git clone https://github.com/YOUR-USERNAME/ScheduleLua.git
   cd ScheduleLua
   ```

3. **Add the Original Repository as Upstream**
   ```bash
   git remote add upstream https://github.com/ScheduleLua/ScheduleLua-Framework.git
   ```

### Making Changes

4. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```
   - Use a descriptive branch name related to your changes
   - Prefix with `feature/`, `bugfix/`, `docs/`, etc. as appropriate

5. **Make Your Changes**
   - Implement your feature or fix
   - Follow the existing code style and conventions
   - Add or update documentation as needed

6. **Test Your Changes**
   - Ensure your changes work as expected
   - Test with the game to verify functionality
   - Check for any unintended side effects

7. **Commit Your Changes**
   ```bash
   git add .
   git commit -m "Add a descriptive commit message"
   ```
   - Use clear and descriptive commit messages
   - Reference issue numbers in commit messages when applicable

### Submitting Your Contribution

8. **Keep Your Branch Updated**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

9. **Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

10. **Create a Pull Request**
    - Go to your fork on GitHub
    - Click "New Pull Request"
    - Select your feature branch
    - Click "Create Pull Request"
    - Fill in the PR template with details about your changes
    - Reference any related issues

11. **Respond to Feedback**
    - Be responsive to comments and feedback
    - Make requested changes and push to your branch
    - The PR will update automatically

### Contribution Guidelines

- **Code Style**: Follow the existing code style in the project
- **Documentation**: Update documentation when adding or changing features
- **Commits**: Keep commits focused and logically separate
- **Testing**: Test your changes thoroughly before submitting
- **Issues**: Create an issue before working on major changes

Thank you for contributing to ScheduleLua!

## License

[![GPL-3.0 License](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](LICENSE)

This project is licensed under the GPL-3.0 License - see the LICENSE file for details.

## Acknowledgments

- [MelonLoader](https://melonwiki.xyz/#/) for the mod loader
- [MoonSharp](https://www.moonsharp.org/) for the Lua interpreter
- [TVGS](https://scheduleonegame.com/) for making Schedule 1
- nica.0355 For their contributions to adding more lua bindings
- All contributors and the modding community
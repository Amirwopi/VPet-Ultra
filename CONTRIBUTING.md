# Contributing

Thank you for your interest in contributing to VPet-Ultra! This is a fork of [VPet](https://github.com/LorisYounger/VPet) with Steam dependencies removed and AI capabilities added.

## How to Contribute

1. **Fork** the repository
2. **Create a branch** for your feature: `git checkout -b feature/my-feature`
3. **Commit** your changes with clear messages
4. **Push** to your fork and submit a **Pull Request**

## Code Style

- C# with .NET 8.0
- Follow the existing code style in the project
- Remove all Chinese comments — use English only
- Use meaningful variable and method names

## Build

```bash
# Linux (with EnableWindowsTargeting):
dotnet build VPet-Simulator.Windows/VPet-Simulator.Windows.csproj -c Release -p:Platform=x64 -p:EnableWindowsTargeting=true

# Windows:
dotnet build VPet-Simulator.Windows/VPet-Simulator.Windows.csproj -c Release -p:Platform=x64
```

## Reporting Issues

- Use [GitHub Issues](https://github.com/Amirwopi/VPet-Ultra/issues)
- Include steps to reproduce, expected vs actual behavior, and logs if available

## License

This project inherits the upstream license. See the original [VPet](https://github.com/LorisYounger/VPet) repository for details.
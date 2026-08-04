# ─────────────────────────────────────────────────────────────────────────────
# VPet-Ultra — CLion toolchain file
# ─────────────────────────────────────────────────────────────────────────────
# This file tells CMake/CLion to NOT look for a C/C++ compiler.
# The VPet-Ultra CMakeLists.txt uses LANGUAGES NONE (pure .NET wrapper),
# so no compiler is needed — only Ninja (bundled with CLion) and dotnet.
#
# Usage:
#   CLion ▸ Settings ▸ Build, Execution, Deployment ▸ Toolchains
#   Add a new toolchain pointing to this file as the "CMake toolchain".
# ─────────────────────────────────────────────────────────────────────────────

# No C/C++ compiler — this is a pure .NET project.
set(CMAKE_C_COMPILER "")
set(CMAKE_CXX_COMPILER "")
set(CMAKE_C_FLAGS "")
set(CMAKE_CXX_FLAGS "")

# Use CLion's bundled Ninja.
set(CMAKE_MAKE_PROGRAM "D:/Program Files/JetBrains/CLion 2025.3.3/bin/ninja/win/x64/ninja.exe"
    CACHE FILEPATH "Path to Ninja")

# No system root needed.
set(CMAKE_SYSROOT "")
# ─────────────────────────────────────────────────────────────────────────────
# VPet-Ultra — CMake wrapper for CLion / .NET 8 solution
# ─────────────────────────────────────────────────────────────────────────────
# This file does NOT compile C/C++ code. It wraps the `dotnet` CLI so CLion
# can:
#   • index the solution structure (16 C# projects)
#   • build / restore / clean / test via custom targets
#   • launch the WPF desktop app or the xUnit test runner
#   • attach netcoredbg for step-through debugging
#
# Requirements:
#   - .NET 8 SDK  (dotnet --version → 8.x)
#   - CMake 3.20+ (CLion ships one)
#   - netcoredbg  (optional, for debugging — see scripts/install-netcoredbg.ps1)
#
# Usage in CLion:
#   1. Open this folder (File ▸ Open ▸ CMakeLists.txt)
#   2. CLion will configure automatically. Pick the "Debug" profile.
#   3. Use the Run Configurations in the toolbar:
#        - Build All          → dotnet build
#        - Restore            → dotnet restore
#        - Run VPet.Windows   → launch the desktop pet (x64 Debug)
#        - Run VPet.Solution   → launch the solution launcher
#        - Tests              → dotnet test
#        - Clean              → dotnet clean
#   4. For step-through debugging, install netcoredbg and pick the
#      "Debug VPet.Windows (netcoredbg)" configuration.
#
# All build artifacts stay inside each project's bin/obj (managed by dotnet).
# CMake's own build directory is only used for the wrapper scripts.
# ─────────────────────────────────────────────────────────────────────────────

cmake_minimum_required(VERSION 3.20)

# ── Project metadata ─────────────────────────────────────────────────────────
project(VPet_Ultra
    VERSION 0.2.0
    DESCRIPTION "VPet-Ultra: virtual desktop pet with offline AI cognition"
    LANGUAGES NONE)          # no C/C++ compiler needed

set(VPET_VERSION "0.2.0-dev")

# ── Tool detection ──────────────────────────────────────────────────────────
# Find the dotnet executable once; fail early with a clear message.
find_program(DOTNET_CMD dotnet
    DOC "Path to the .NET SDK executable (dotnet)"
    REQUIRED)

# Capture the installed SDK version for logging / sanity checks.
execute_process(
    COMMAND ${DOTNET_CMD} --version
    OUTPUT_VARIABLE DOTNET_SDK_VERSION
    OUTPUT_STRIP_TRAILING_WHITESPACE
    ERROR_QUIET)

message(STATUS "VPet-Ultra: dotnet found at ${DOTNET_CMD}")
message(STATUS "VPet-Ultra: .NET SDK version ${DOTNET_SDK_VERSION}")

# netcoredbg is optional — only needed for step-through debugging.
find_program(NETCOREDBG_CMD netcoredbg
    DOC "Path to netcoredbg (optional, for .NET debugging in CLion)")
if(NETCOREDBG_CMD)
    message(STATUS "VPet-Ultra: netcoredbg found at ${NETCOREDBG_CMD}")
else()
    message(STATUS "VPet-Ultra: netcoredbg NOT found — run scripts/install-netcoredbg.ps1 to enable debugging")
endif()

# ── Configuration ───────────────────────────────────────────────────────────
# CLion passes -DCMAKE_BUILD_TYPE=Debug|Release. Map it to dotnet -c.
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Debug CACHE STRING "Build type" FORCE)
    set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS Debug Release)
endif()

# Default platform for the Windows desktop project (x64|x86|Any CPU).
set(VPET_PLATFORM "x64" CACHE STRING "Platform for VPet-Simulator.Windows (x64|x86|Any CPU)")
set_property(CACHE VPET_PLATFORM PROPERTY STRINGS x64 x86 "Any CPU")

# Solution file path (relative to this CMakeLists.txt).
set(VPET_SLN "${CMAKE_CURRENT_SOURCE_DIR}/VPet.sln")

# Output directory for wrapper scripts / launchers.
set(VPET_BUILD_DIR "${CMAKE_BINARY_DIR}/vpet-build")
file(MAKE_DIRECTORY ${VPET_BUILD_DIR})

# ── Helper: define a dotnet-backed custom target ───────────────────────────
# vpet_dotnet_target(<name> <description> COMMAND <args...>)
function(vpet_dotnet_target name desc)
    set(args ${ARGN})
    add_custom_target(${name}
        COMMAND ${args}
        WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
        VERBATIM
        COMMENT "${desc}"
        USES_TERMINAL)
endfunction()

# ── Core build targets ─────────────────────────────────────────────────────

# restore — dotnet restore (NuGet + project refs)
vpet_dotnet_target(restore
    "Restoring NuGet packages for VPet.sln"
    COMMAND ${DOTNET_CMD} restore ${VPET_SLN})

# build-all — dotnet build (whole solution). Restore first so a fresh
# checkout builds without a separate "restore" step.
vpet_dotnet_target(build-all
    "Building VPet.sln (${CMAKE_BUILD_TYPE})"
    COMMAND ${DOTNET_CMD} restore ${VPET_SLN}
    COMMAND ${DOTNET_CMD} build ${VPET_SLN}
            -c ${CMAKE_BUILD_TYPE}
            -p:Platform=${VPET_PLATFORM}
            --no-restore)

# clean-all — dotnet clean (CMake reserves the "clean" target name)
vpet_dotnet_target(clean-all
    "Cleaning VPet.sln"
    COMMAND ${DOTNET_CMD} clean ${VPET_SLN}
            -c ${CMAKE_BUILD_TYPE}
            -p:Platform=${VPET_PLATFORM})

# test — dotnet test (cognition unit tests)
vpet_dotnet_target(test
    "Running VPet.Cognition.Tests (xUnit)"
    COMMAND ${DOTNET_CMD} test
            ${CMAKE_CURRENT_SOURCE_DIR}/VPet.Cognition.Tests/VPet.Cognition.Tests.csproj
            -c ${CMAKE_BUILD_TYPE}
            --no-build)

# test-build — build then test in one step
add_custom_target(test-build
    COMMAND ${CMAKE_COMMAND} --build ${CMAKE_BINARY_DIR} --target build-all
    COMMAND ${DOTNET_CMD} test
            ${CMAKE_CURRENT_SOURCE_DIR}/VPet.Cognition.Tests/VPet.Cognition.Tests.csproj
            -c ${CMAKE_BUILD_TYPE}
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    VERBATIM
    COMMENT "Build + test VPet.Cognition.Tests"
    USES_TERMINAL)

# ── Per-project build targets ──────────────────────────────────────────────
# Each C# project gets its own target so you can build a single project
# from the CLion target list without rebuilding everything.

set(VPET_PROJECTS
    VPet-Simulator.Core
    VPet-Simulator.Windows.Interface
    VPet-Simulator.Windows
    VPet-Simulator.Tool
    VPet.Solution
    VPet.Cognition.Domain
    VPet.Cognition.LLM
    VPet.Cognition.Memory
    VPet.Cognition.Voice
    VPet.Cognition.Personality
    VPet.Cognition.Agent
    VPet.Cognition.Security
    VPet.Cognition.Inference
    VPet.Cognition.Wpf
    VPet.Cognition.Host
    VPet.Cognition.Tests)

foreach(proj ${VPET_PROJECTS})
    set(csproj "${CMAKE_CURRENT_SOURCE_DIR}/${proj}/${proj}.csproj")
    # Sanitize target name (CMake targets can't have hyphens).
    string(REPLACE "-" "_" target_name ${proj})
    vpet_dotnet_target(build-${target_name}
        "Building ${proj} (${CMAKE_BUILD_TYPE})"
        COMMAND ${DOTNET_CMD} restore ${csproj}
        COMMAND ${DOTNET_CMD} build ${csproj}
                -c ${CMAKE_BUILD_TYPE}
                -p:Platform=${VPET_PLATFORM}
                --no-restore)
endforeach()

# ── Run targets (launch the apps) ──────────────────────────────────────────
# These produce a wrapper script in the build dir so CLion can "run" them
# as if they were native executables.

# VPet-Simulator.Windows — the main desktop pet (WPF, x64)
set(WINDOWS_EXE
    "${CMAKE_CURRENT_SOURCE_DIR}/VPet-Simulator.Windows/bin/${VPET_PLATFORM}/${CMAKE_BUILD_TYPE}/net8.0-windows/VPet-Simulator.Windows.exe")

# VPet.Solution — the solution launcher (WPF, Any CPU)
set(SOLUTION_EXE
    "${CMAKE_CURRENT_SOURCE_DIR}/VPet.Solution/bin/${CMAKE_BUILD_TYPE}/net8.0-windows/VPet.Solution.exe")

# VPet-Simulator.Tool — console tool (net48)
set(TOOL_EXE
    "${CMAKE_CURRENT_SOURCE_DIR}/VPet-Simulator.Tool/bin/${CMAKE_BUILD_TYPE}/net48/VPet-Simulator.Tool.exe")

# Generate a PowerShell wrapper that builds then launches a given exe.
function(vpet_run_target name desc exe_path csproj_path)
    set(script "${VPET_BUILD_DIR}/run-${name}.ps1")
    file(WRITE ${script}
"# Auto-generated by CMake — run ${name} from CLion\n"
"# Builds the project then launches the executable.\n"
"\$ErrorActionPreference = 'Stop'\n"
"Write-Host '[${name}] Building ${csproj_path}...' -ForegroundColor Cyan\n"
"& '${DOTNET_CMD}' build '${csproj_path}' -c ${CMAKE_BUILD_TYPE} -p:Platform=${VPET_PLATFORM} --no-restore\n"
"if (\$LASTEXITCODE -ne 0) { Write-Host 'Build failed' -ForegroundColor Red; exit 1 }\n"
"Write-Host '[${name}] Launching ${exe_path}' -ForegroundColor Green\n"
"& '${exe_path}' \$args\n")
    add_custom_target(run-${name}
        COMMAND powershell -NoProfile -ExecutionPolicy Bypass -File ${script}
        WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
        VERBATIM
        COMMENT "${desc}"
        USES_TERMINAL)
endfunction()

vpet_run_target(VPet.Windows
    "Run VPet-Simulator.Windows (desktop pet)"
    "${WINDOWS_EXE}"
    "${CMAKE_CURRENT_SOURCE_DIR}/VPet-Simulator.Windows/VPet-Simulator.Windows.csproj")

vpet_run_target(VPet.Solution
    "Run VPet.Solution (solution launcher)"
    "${SOLUTION_EXE}"
    "${CMAKE_CURRENT_SOURCE_DIR}/VPet.Solution/VPet.Solution.csproj")

vpet_run_target(VPet.Tool
    "Run VPet-Simulator.Tool (console tool)"
    "${TOOL_EXE}"
    "${CMAKE_CURRENT_SOURCE_DIR}/VPet-Simulator.Tool/VPet-Simulator.Tool.csproj")

# ── Debug targets (netcoredbg) ─────────────────────────────────────────────
# If netcoredbg is installed, generate launch scripts that start the app
# under the debugger. CLion can attach to these via "Run with debugger".
if(NETCOREDBG_CMD)
    function(vpet_debug_target name desc exe_path csproj_path)
        set(script "${VPET_BUILD_DIR}/debug-${name}.ps1")
        file(WRITE ${script}
"# Auto-generated by CMake — debug ${name} with netcoredbg\n"
"\$ErrorActionPreference = 'Stop'\n"
"Write-Host '[debug ${name}] Building...' -ForegroundColor Cyan\n"
"& '${DOTNET_CMD}' build '${csproj_path}' -c ${CMAKE_BUILD_TYPE} -p:Platform=${VPET_PLATFORM} --no-restore\n"
"if (\$LASTEXITCODE -ne 0) { Write-Host 'Build failed' -ForegroundColor Red; exit 1 }\n"
"Write-Host '[debug ${name}] Starting netcoredbg...' -ForegroundColor Green\n"
"& '${NETCOREDBG_CMD}' --interpreter=cli -- '${exe_path}'\n")
        add_custom_target(debug-${name}
            COMMAND powershell -NoProfile -ExecutionPolicy Bypass -File ${script}
            WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
            VERBATIM
            COMMENT "${desc}"
            USES_TERMINAL)
    endfunction()

    vpet_debug_target(VPet.Windows
        "Debug VPet-Simulator.Windows with netcoredbg"
        "${WINDOWS_EXE}"
        "${CMAKE_CURRENT_SOURCE_DIR}/VPet-Simulator.Windows/VPet-Simulator.Windows.csproj")

    vpet_debug_target(VPet.Solution
        "Debug VPet.Solution with netcoredbg"
        "${SOLUTION_EXE}"
        "${CMAKE_CURRENT_SOURCE_DIR}/VPet.Solution/VPet.Solution.csproj")
endif()

# ── Executable targets (for CLion Debug button) ─────────────────────────────
# CLion's Debug button needs a real executable target (not just a custom
# target). We declare IMPORTED executables pointing to the .NET output.
# CLion treats these as native executables and lets you set breakpoints,
# step through code, and inspect variables (via netcoredbg or the GDB/LLDB
# wrapper).
#
# These targets do NOT build anything — the build happens via the custom
# targets above (build-all, run-*, etc.). They only tell CLion "here is the
# binary to launch/debug".

# VPet-Simulator.Windows — main desktop pet (WPF, x64)
add_executable(VPet_Windows IMPORTED)
set_target_properties(VPet_Windows PROPERTIES
    IMPORTED_LOCATION "${WINDOWS_EXE}"
    IMPORTED_LOCATION_DEBUG "${WINDOWS_EXE}"
    IMPORTED_LOCATION_RELEASE "${WINDOWS_EXE}")

# VPet.Solution — solution launcher (WPF, Any CPU)
add_executable(VPet_Solution IMPORTED)
set_target_properties(VPet_Solution PROPERTIES
    IMPORTED_LOCATION "${SOLUTION_EXE}"
    IMPORTED_LOCATION_DEBUG "${SOLUTION_EXE}"
    IMPORTED_LOCATION_RELEASE "${SOLUTION_EXE}")

# VPet-Simulator.Tool — console tool (net48)
add_executable(VPet_Tool IMPORTED)
set_target_properties(VPet_Tool PROPERTIES
    IMPORTED_LOCATION "${TOOL_EXE}"
    IMPORTED_LOCATION_DEBUG "${TOOL_EXE}"
    IMPORTED_LOCATION_RELEASE "${TOOL_EXE}")

# ── Publish target (full build + package) ───────────────────────────────────
# Runs build-local.ps1 with the current CMake build type (Debug or Release).
# This builds the solution, copies mod data, Live2D, Piper, Whisper, etc.
vpet_dotnet_target(publish
    "Publishing VPet-Ultra (build-local.ps1, ${CMAKE_BUILD_TYPE})"
    COMMAND powershell -NoProfile -ExecutionPolicy Bypass
            -File ${CMAKE_CURRENT_SOURCE_DIR}/build-local.ps1
            -Configuration ${CMAKE_BUILD_TYPE}
            -Platform ${VPET_PLATFORM})

# ── Run-Debug target (publish in Debug + launch) ───────────────────────────
# This is the main "build everything and run" target for debugging.
# It runs build-local.ps1 in Debug mode (which builds all DLLs, copies
# mod/0000_core, mod/0000_cognition, Live2D, Piper, Whisper, appsettings.json),
# then launches VPet-Simulator.Windows.exe from the publish directory.
set(PUBLISH_EXE
    "${CMAKE_CURRENT_SOURCE_DIR}/publish/vpet-ultra/VPet-Simulator.Windows.exe")

set(run_debug_script "${VPET_BUILD_DIR}/run-debug.ps1")
file(WRITE ${run_debug_script}
"# Auto-generated by CMake — publish + run VPet in Debug mode\n"
"\$ErrorActionPreference = 'Stop'\n"
"Write-Host '[run-debug] Publishing (build-local.ps1 -Configuration Debug)...' -ForegroundColor Cyan\n"
"& powershell -NoProfile -ExecutionPolicy Bypass -File '${CMAKE_CURRENT_SOURCE_DIR}/build-local.ps1' -Configuration Debug -Platform ${VPET_PLATFORM}\n"
"if (\$LASTEXITCODE -ne 0) { Write-Host 'Publish failed' -ForegroundColor Red; exit 1 }\n"
"Write-Host '[run-debug] Launching ${PUBLISH_EXE}' -ForegroundColor Green\n"
"& '${PUBLISH_EXE}' \$args\n")

add_custom_target(run-debug
    COMMAND powershell -NoProfile -ExecutionPolicy Bypass -File ${run_debug_script}
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    VERBATIM
    COMMENT "Publish (Debug) + run VPet-Simulator.Windows with all mods/DLLs"
    USES_TERMINAL)

# ── Aggregate "all" target ──────────────────────────────────────────────────
# CLion's default "Build" action builds the ALL target. Make it restore +
# build the whole solution.
add_custom_target(all-build
    COMMAND ${DOTNET_CMD} restore ${VPET_SLN}
    COMMAND ${DOTNET_CMD} build ${VPET_SLN} -c ${CMAKE_BUILD_TYPE} -p:Platform=${VPET_PLATFORM} --no-restore
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    VERBATIM
    COMMENT "Restore + build VPet.sln (${CMAKE_BUILD_TYPE}, ${VPET_PLATFORM})"
    USES_TERMINAL)

# Make ALL depend on the solution build (so CLion's Build ▸ Build Project works).
add_dependencies(all-build build-all)

# ── Status summary ──────────────────────────────────────────────────────────
message(STATUS "VPet-Ultra: configuration = ${CMAKE_BUILD_TYPE}")
message(STATUS "VPet-Ultra: platform      = ${VPET_PLATFORM}")
message(STATUS "VPet-Ultra: solution      = ${VPET_SLN}")
message(STATUS "VPet-Ultra: build dir     = ${VPET_BUILD_DIR}")
message(STATUS "VPet-Ultra: targets:")
message(STATUS "    restore, build-all, clean-all, test, test-build, publish")
message(STATUS "    build-<project>  (16 per-project targets)")
message(STATUS "    run-VPet.Windows, run-VPet.Solution, run-VPet.Tool")
message(STATUS "    run-debug        (publish Debug + launch with all mods/DLLs)")
if(NETCOREDBG_CMD)
    message(STATUS "    debug-VPet.Windows, debug-VPet.Solution (netcoredbg)")
endif()
message(STATUS "")
message(STATUS "VPet-Ultra: CMake configuration complete.")
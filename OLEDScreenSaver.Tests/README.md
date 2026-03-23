# OLEDScreenSaver Tests

This test project contains unit tests for the OLEDScreenSaver application.

## Running Tests

### Visual Studio
1. Open the solution in Visual Studio
2. Build the solution (Ctrl+Shift+B)
3. Open Test Explorer (Test > Test Explorer)
4. Click "Run All Tests" or run individual test classes/methods

### Command Line (MSBuild)
```bash
msbuild OLEDScreenSaver.sln /t:Build
vstest.console.exe OLEDScreenSaver.Tests\bin\Debug\OLEDScreenSaver.Tests.dll
```

## Test Coverage

### RegistryHelperTests
- Saving and loading timeout values
- Validation of timeout values (positive numbers)
- Saving and loading second stage timeout
- Zero value handling for second stage timeout
- Saving and loading poll rate
- Saving and loading screen names
- Culture-invariant decimal parsing (important for international users)
- Small decimal values (e.g., 0.1)

### ScreenSaverTests
- ScreenSaver initialization
- Pause and resume functionality
- Mouse activity notification
- Timeout updates
- Multiple screen tracking
- Proper disposal

### ConfigFormTests
- ConfigForm creation
- Form initialization with registry values
- Timeout validation logic
- Second stage timeout validation (allows zero)
- Small decimal value parsing
- Culture-invariant parsing

## Notes

- Some tests require registry access and may need to be run with appropriate permissions
- UI-related tests are limited due to the complexity of testing Windows Forms without showing windows
- Tests use MSTest framework (Microsoft.VisualStudio.TestTools.UnitTesting)

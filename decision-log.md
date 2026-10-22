CYS.Extensions.Hosting.Testing - Decision Log

## Why isn't code coverage 100% for this library.

DUE TO CONFIRMED MICROSOFT COMPILER SHORT-COMINGS, THIS LIBRARY WILL NEVER HAVE 100% CODE COVERAGE
WITHOUT EXCLUDING THE InvokeEntryPointAsync METHOD FROM CODE COVERAGE.

- (CONFIRMED BY HUMAN) - HostApplicationFactory.InvokeEntryPointAsync has a throw; statement that
  is required by the compiler, but is unreachable in actual execution paths.


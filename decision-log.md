CYS.Extensions.Hosting.Testing - Decision Log

## Decision: Exclude .NET 9 from AltCover Coverage Runs

### Context

An intermittent `ObjectDisposedException` occurs when the test suite is instrumented by AltCover and executed against .NET 9 on Linux CI agents.

The same tests pass:

- against .NET 9 without AltCover,
- against .NET 9 on Windows,
- against .NET 8 with AltCover, and
- against .NET 10 with AltCover.

The affected production code contains a lock-protected, permanent guard that prevents `RunAsync()` from progressing after its first invocation. The observed failure is therefore specific to the instrumented .NET 9 Linux execution environment.

### Decision

The normal test suite will continue to run against .NET 9.

AltCover-instrumented execution will skip the .NET 9 target. Coverage will be collected from the other supported target frameworks.

### Alternatives Rejected

The following alternatives were rejected:

- adding retries to the affected tests,
- adding delays or additional synchronization to production code,
- accepting `ObjectDisposedException` as a valid test result,
- weakening the expected `InvalidOperationException`, and
- investing in a workaround for a runtime nearing the end of its support lifetime.

Each alternative would either conceal the instrumentation race, weaken the tested contract, or introduce production complexity without a corresponding production defect.

### Consequences

- .NET 9 compatibility remains validated by the normal test run.
- AltCover coverage collection excludes the .NET 9 target.
- Coverage continues to be collected from the remaining supported target frameworks.
- Production code and tests will not be modified to accommodate the instrumentation race.
- Coverage results for .NET 9 will not be produced.


## Why isn't code coverage 100% for this library.

DUE TO CONFIRMED MICROSOFT COMPILER SHORT-COMINGS, THIS LIBRARY WILL NEVER HAVE 100% CODE COVERAGE
WITHOUT EXCLUDING THE InvokeEntryPointAsync METHOD FROM CODE COVERAGE.

- (CONFIRMED BY HUMAN) - HostApplicationFactory.InvokeEntryPointAsync has a throw; statement that
  is required by the compiler, but is unreachable in actual execution paths.


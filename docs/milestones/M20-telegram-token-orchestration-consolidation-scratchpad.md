# M20 Scratchpad

## Notes
- `dotnet build HeyAlan/HeyAlan.csproj` succeeds after refactor.
- `dotnet test HeyAlan.Tests/HeyAlan.Tests.csproj` with normal build is currently blocked by a file lock on `HeyAlan.WebApi/bin/Debug/net10.0/HeyAlan.dll` (locked by Visual Studio + running `HeyAlan.WebApi` process).
- `dotnet test --no-build` executes but reports existing failures not introduced by this milestone work:
  - `IdentityEndpointsSecurityTests.IsActiveSubscriptionOnboardedAsync_WhenActiveCompleted_ReturnsTrue`
  - `IdentityEndpointsSecurityTests.IsActiveSubscriptionOnboardedAsync_WhenActiveIncompleteAndAnotherCompleted_ReturnsFalse`
  - `SubscriptionOnboardingServiceTests.GetStateAsync_WhenDraft_ReturnsSquareConnectCurrentStep`
  - `SubscriptionOnboardingServiceTests.FinalizeAsync_WhenInvitationsNotCompleted_ReturnsValidationError`
  - `SubscriptionOnboardingServiceTests.RecomputeStateAsync_WhenAllChannelsInvalidated_FallsBackToInProgress`

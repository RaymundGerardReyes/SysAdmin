#!/usr/bin/env bash
# ==============================================================================
# PipelineSolution Modular Scaffolder: Root Solution & Governance
# ==============================================================================

set -euo pipefail

write_file() {
  local target="$1"
  local content="$2"
  local force="${3:-false}"

  if [[ -f "$target" ]] && [[ "$force" != "true" ]]; then
    return 0
  fi

  mkdir -p "$(dirname "$target")"
  printf '%s\n' "$content" > "$target"
}

scaffold_solution_governance() {
  local force="${1:-false}"

  # 1. global.json
  write_file "global.json" '{
  "sdk": {
    "version": "10.0.301",
    "rollForward": "latestFeature"
  }
}' "$force"

  # 2. Directory.Build.props
  write_file "Directory.Build.props" '<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <NoWarn>$(NoWarn);CA1711;CA1000;CA1859;CA1707;CA1305;CA1822;CA1850</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <!-- Suppress transitive SQLite native binary advisory until upstream SQLitePCLRaw bundles patched SQLite -->
    <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-2m69-gcr7-jv3q" />
    <NuGetAuditSuppress Include="GHSA-2m69-gcr7-jv3q" />
  </ItemGroup>

  <!-- Internals visibility for unit, security, integration and architecture test projects -->
  <ItemGroup Condition="'\''$(MSBuildProjectName)'\'' == '\''Pipeline.Application'\''>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Application.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Security.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Architecture.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.TestKit</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>DynamicProxyGenAssembly2</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

  <ItemGroup Condition="'\''$(MSBuildProjectName)'\'' == '\''Pipeline.Infrastructure'\''>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Infrastructure.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Integration.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Security.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Architecture.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Desktop.Host</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.LocalApi</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.TestKit</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>DynamicProxyGenAssembly2</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

  <ItemGroup Condition="'\''$(MSBuildProjectName)'\'' == '\''Pipeline.Desktop.Host'\''>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Security.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Architecture.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

  <ItemGroup Condition="'\''$(MSBuildProjectName)'\'' == '\''Pipeline.LocalApi'\''>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Api.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Security.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Architecture.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

  <ItemGroup Condition="$(MSBuildProjectName.StartsWith('\''Integrations.'\''))">
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Integration.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Contract.Tests</_Parameter1>
    </AssemblyAttribute>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Pipeline.Architecture.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
</Project>' "$force"

  # 3. Directory.Packages.props
  write_file "Directory.Packages.props" '<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <!-- Microsoft Extensions & Core -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageVersion Include="System.Security.Cryptography.ProtectedData" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Data.Sqlite" Version="10.0.0" />

    <!-- Testing Toolchain -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.2" />
    <PackageVersion Include="Shouldly" Version="4.3.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="NSubstitute.Analyzers.CSharp" Version="1.0.17" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="Verify.Xunit" Version="28.13.0" />
    <PackageVersion Include="FsCheck.Xunit" Version="2.16.6" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageVersion Include="WireMock.Net" Version="1.7.4" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>
</Project>' "$force"

  # 4. PipelineSolution.slnx
  write_file "PipelineSolution.slnx" '<Solution>
  <Folder Name="/src/">
    <Project Path="src/Pipeline.Contracts/Pipeline.Contracts.csproj" />
    <Project Path="src/Pipeline.Domain/Pipeline.Domain.csproj" />
    <Project Path="src/Pipeline.Application/Pipeline.Application.csproj" />
    <Project Path="src/Pipeline.Infrastructure/Pipeline.Infrastructure.csproj" />
    <Project Path="src/Pipeline.Desktop.Host/Pipeline.Desktop.Host.csproj" />
    <Project Path="src/Pipeline.LocalApi/Pipeline.LocalApi.csproj" />
    <Project Path="src/Integrations.Google.Sheets/Integrations.Google.Sheets.csproj" />
    <Project Path="src/Integrations.Google.Gmail/Integrations.Google.Gmail.csproj" />
    <Project Path="src/Integrations.Google.Chat/Integrations.Google.Chat.csproj" />
    <Project Path="src/Integrations.Zoho.Expense/Integrations.Zoho.Expense.csproj" />
    <Project Path="src/Integrations.Zoho.Bigin/Integrations.Zoho.Bigin.csproj" />
    <Project Path="src/Integrations.PayMongo/Integrations.PayMongo.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Pipeline.TestKit/Pipeline.TestKit.csproj" />
    <Project Path="tests/Pipeline.Domain.Tests/Pipeline.Domain.Tests.csproj" />
    <Project Path="tests/Pipeline.Application.Tests/Pipeline.Application.Tests.csproj" />
    <Project Path="tests/Pipeline.Architecture.Tests/Pipeline.Architecture.Tests.csproj" />
    <Project Path="tests/Pipeline.Security.Tests/Pipeline.Security.Tests.csproj" />
    <Project Path="tests/Pipeline.Integration.Tests/Pipeline.Integration.Tests.csproj" />
    <Project Path="tests/Pipeline.Contract.Tests/Pipeline.Contract.Tests.csproj" />
    <Project Path="tests/Pipeline.Api.Tests/Pipeline.Api.Tests.csproj" />
    <Project Path="tests/Pipeline.Regression.Tests/Pipeline.Regression.Tests.csproj" />
  </Folder>
</Solution>' "$force"

  # 5. .gitignore
  write_file ".gitignore" 'bin/
obj/
TestResults/
*.user
*.suo
.vs/
.vscode/
node_modules/
dist/
coverage/
.test_write
*.log
.DS_Store' "$force"

  # 6. CI Workflow
  write_file ".github/workflows/ci.yml" 'name: CI

on:
  push:
    branches: [ main, master ]
  pull_request:
    branches: [ main, master ]

jobs:
  backend:
    name: Backend .NET 10 Verification
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - name: Restore
        run: dotnet restore PipelineSolution.slnx
      - name: Build
        run: dotnet build PipelineSolution.slnx --no-restore -warnaserror
      - name: Test
        run: dotnet test PipelineSolution.slnx --no-build --verbosity normal

  frontend:
    name: Frontend React Native Verification
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "22.x"
      - name: Install presentation dependencies
        working-directory: src/Pipeline.Presentation
        run: npm ci || npm install
      - name: Verify structure
        working-directory: src/Pipeline.Presentation
        run: npm run check:structure
      - name: Verify contract drift
        working-directory: src/Pipeline.Presentation
        run: npm run check:contracts
      - name: Verify traceability
        working-directory: src/Pipeline.Presentation
        run: npm run check:trace
      - name: Typecheck
        working-directory: src/Pipeline.Presentation
        run: npm run typecheck
      - name: Run Jest tests
        working-directory: src/Pipeline.Presentation
        run: npm test -- --ci' "$force"
}

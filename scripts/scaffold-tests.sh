#!/usr/bin/env bash
# ==============================================================================
# PipelineSolution Modular Scaffolder: Backend Test Projects & Test Suites
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

scaffold_test_projects() {
  local force="${1:-false}"

  # 1. Pipeline.TestKit.csproj
  write_file "tests/Pipeline.TestKit/Pipeline.TestKit.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Application\Pipeline.Application.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Infrastructure\Pipeline.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>
</Project>' "$force"

  # 2. Pipeline.Domain.Tests.csproj
  write_file "tests/Pipeline.Domain.Tests/Pipeline.Domain.Tests.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\Pipeline.TestKit\Pipeline.TestKit.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="FsCheck.Xunit" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>
</Project>' "$force"

  # 3. Pipeline.Application.Tests.csproj
  write_file "tests/Pipeline.Application.Tests/Pipeline.Application.Tests.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Application\Pipeline.Application.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Infrastructure\Pipeline.Infrastructure.csproj" />
    <ProjectReference Include="..\Pipeline.TestKit\Pipeline.TestKit.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" />
    <PackageReference Include="FsCheck.Xunit" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>
  <ItemGroup>
    <None Update="Fixtures\**">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>' "$force"

  # 4. Pipeline.Architecture.Tests.csproj
  write_file "tests/Pipeline.Architecture.Tests/Pipeline.Architecture.Tests.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Application\Pipeline.Application.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Infrastructure\Pipeline.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Desktop.Host\Pipeline.Desktop.Host.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.LocalApi\Pipeline.LocalApi.csproj" />
    <ProjectReference Include="..\..\src\Integrations.Google.Sheets\Integrations.Google.Sheets.csproj" />
    <ProjectReference Include="..\..\src\Integrations.Google.Gmail\Integrations.Google.Gmail.csproj" />
    <ProjectReference Include="..\..\src\Integrations.Google.Chat\Integrations.Google.Chat.csproj" />
    <ProjectReference Include="..\..\src\Integrations.Zoho.Expense\Integrations.Zoho.Expense.csproj" />
    <ProjectReference Include="..\..\src\Integrations.Zoho.Bigin\Integrations.Zoho.Bigin.csproj" />
    <ProjectReference Include="..\..\src\Integrations.PayMongo\Integrations.PayMongo.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="NetArchTest.Rules" />
  </ItemGroup>
</Project>' "$force"

  # 5. Pipeline.Security.Tests.csproj
  write_file "tests/Pipeline.Security.Tests/Pipeline.Security.Tests.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Application\Pipeline.Application.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Infrastructure\Pipeline.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Desktop.Host\Pipeline.Desktop.Host.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.LocalApi\Pipeline.LocalApi.csproj" />
    <ProjectReference Include="..\Pipeline.TestKit\Pipeline.TestKit.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>
</Project>' "$force"

  # 6. Pipeline.Integration.Tests.csproj
  write_file "tests/Pipeline.Integration.Tests/Pipeline.Integration.Tests.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Application\Pipeline.Application.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Infrastructure\Pipeline.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Integrations.Google.Sheets\Integrations.Google.Sheets.csproj" />
    <ProjectReference Include="..\Pipeline.TestKit\Pipeline.TestKit.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.Data.Sqlite" />
  </ItemGroup>
</Project>' "$force"

  # 7. Pipeline.Contract.Tests.csproj
  write_file "tests/Pipeline.Contract.Tests/Pipeline.Contract.Tests.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Application\Pipeline.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>
</Project>' "$force"

  # 8. Pipeline.Api.Tests.csproj
  write_file "tests/Pipeline.Api.Tests/Pipeline.Api.Tests.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Application\Pipeline.Application.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.LocalApi\Pipeline.LocalApi.csproj" />
    <ProjectReference Include="..\Pipeline.TestKit\Pipeline.TestKit.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  </ItemGroup>
</Project>' "$force"

  # 9. Pipeline.Regression.Tests.csproj
  write_file "tests/Pipeline.Regression.Tests/Pipeline.Regression.Tests.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\..\src\Pipeline.Contracts\Pipeline.Contracts.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>
  <ItemGroup>
    <None Update="Snapshots\**">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>' "$force"

  # Role-permission matrix fixture
  write_file "tests/Pipeline.Application.Tests/Fixtures/role_permission_matrix.json" '{
  "Viewer": [
    "jobs.read",
    "sheets.values.read",
    "bigin.contacts.read"
  ],
  "Operator": [
    "jobs.read",
    "jobs.cancel",
    "sheets.values.read",
    "sheets.values.write",
    "gmail.messages.send",
    "chat.messages.create",
    "bigin.contacts.read",
    "bigin.contacts.create"
  ],
  "FinanceOperator": [
    "jobs.read",
    "jobs.cancel",
    "sheets.values.read",
    "sheets.values.write",
    "zoho.expense.read",
    "zoho.expense.create",
    "payments.status.read",
    "payments.checkout.create"
  ],
  "Approver": [
    "jobs.read",
    "jobs.cancel",
    "sheets.values.read",
    "zoho.expense.read",
    "payments.status.read",
    "payments.checkout.approve"
  ],
  "IntegrationAdmin": [
    "jobs.read",
    "integrations.connect",
    "integrations.disconnect"
  ],
  "Auditor": [
    "jobs.read",
    "admin.audit.read"
  ],
  "Owner": [
    "jobs.read",
    "jobs.cancel",
    "sheets.values.read",
    "sheets.values.write",
    "gmail.messages.send",
    "chat.messages.create",
    "zoho.expense.read",
    "zoho.expense.create",
    "bigin.contacts.read",
    "bigin.contacts.create",
    "payments.status.read",
    "integrations.connect",
    "integrations.disconnect",
    "admin.users.manage",
    "admin.policy.update",
    "admin.audit.read"
  ]
}' "$force"
}

scaffold_test_suites() {
  local force="${1:-false}"
  scaffold_test_projects "$force"
}

// PipelineApp.cpp : Defines the entry point for the application.
//

#include "pch.h"
#include "PipelineApp.h"

#include "AutolinkedNativeModules.g.h"

#include "NativeModules.h"

// A PackageProvider containing any turbo modules you define within this app project
struct CompReactPackageProvider
    : winrt::implements<CompReactPackageProvider, winrt::Microsoft::ReactNative::IReactPackageProvider> {
 public: // IReactPackageProvider
  void CreatePackage(winrt::Microsoft::ReactNative::IReactPackageBuilder const &packageBuilder) noexcept {
    AddAttributedModules(packageBuilder, true);
  }
};

// The entry point of the Win32 application
_Use_decl_annotations_ int CALLBACK WinMain(HINSTANCE instance, HINSTANCE, PSTR /* commandLine */, int showCmd) {
  // Initialize WinRT
  winrt::init_apartment(winrt::apartment_type::single_threaded);

  // Enable per monitor DPI scaling
  SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

  // Find the path hosting the app exe file
  WCHAR appDirectory[MAX_PATH];
  GetModuleFileNameW(NULL, appDirectory, MAX_PATH);
  PathCchRemoveFileSpec(appDirectory, MAX_PATH);

  // Create a ReactNativeWin32App with the ReactNativeAppBuilder
  auto reactNativeWin32App{winrt::Microsoft::ReactNative::ReactNativeAppBuilder().Build()};

  // Configure the initial InstanceSettings for the app's ReactNativeHost
  auto settings{reactNativeWin32App.ReactNativeHost().InstanceSettings()};
  // Register any autolinked native modules
  RegisterAutolinkedNativeModulePackages(settings.PackageProviders());
  // Register any native modules defined within this app project
  settings.PackageProviders().Append(winrt::make<CompReactPackageProvider>());

#if BUNDLE
  // Load the JS bundle from a file (not Metro):
  // Set the path (on disk) where the .bundle file is located
  settings.BundleRootPath(std::wstring(L"file://").append(appDirectory).append(L"\\Bundle\\").c_str());
  // Set the name of the bundle file (without the .bundle extension)
  settings.JavaScriptBundleFile(L"index.windows");
  // Disable hot reload
  settings.UseFastRefresh(false);
#else
  // Load the JS bundle from Metro
  settings.JavaScriptBundleFile(L"index");
  // Enable hot reload
  settings.UseFastRefresh(true);
#endif
#if _DEBUG
  // For Debug builds
  // Enable Direct Debugging of JS
  settings.UseDirectDebugger(true);
  // Enable the Developer Menu
  settings.UseDeveloperSupport(true);
#else
  // For Release builds:
  // Disable Direct Debugging of JS
  settings.UseDirectDebugger(false);
  // Disable the Developer Menu
  settings.UseDeveloperSupport(false);
#endif

  // Get the AppWindow so we can configure its initial title, size, and modern dark-slate title bar
  auto appWindow{reactNativeWin32App.AppWindow()};
  appWindow.Title(L"Pipeline — Admin Console");
  appWindow.Resize({1200, 850});

  if (winrt::Microsoft::UI::Windowing::AppWindowTitleBar::IsCustomizationSupported()) {
    auto titleBar{appWindow.TitleBar()};
    winrt::Windows::UI::Color surfaceDark{255, 24, 24, 27};      // #18181B (canvas-900)
    winrt::Windows::UI::Color surfaceHover{255, 39, 39, 42};     // #27272A (canvas-800)
    winrt::Windows::UI::Color surfacePressed{255, 63, 63, 70};   // #3F3F46 (canvas-700)
    winrt::Windows::UI::Color textLight{255, 250, 250, 250};     // #FAFAFA (canvas-50)
    winrt::Windows::UI::Color textMuted{255, 161, 161, 170};     // #A1A1AA (canvas-400)

    titleBar.BackgroundColor(surfaceDark);
    titleBar.ForegroundColor(textLight);
    titleBar.InactiveBackgroundColor(surfaceDark);
    titleBar.InactiveForegroundColor(textMuted);

    titleBar.ButtonBackgroundColor(surfaceDark);
    titleBar.ButtonForegroundColor(textLight);
    titleBar.ButtonHoverBackgroundColor(surfaceHover);
    titleBar.ButtonHoverForegroundColor(textLight);
    titleBar.ButtonPressedBackgroundColor(surfacePressed);
    titleBar.ButtonPressedForegroundColor(textLight);
    titleBar.ButtonInactiveBackgroundColor(surfaceDark);
    titleBar.ButtonInactiveForegroundColor(textMuted);
  }

  // Get the ReactViewOptions so we can set the initial RN component to load
  auto viewOptions{reactNativeWin32App.ReactViewOptions()};
  viewOptions.ComponentName(L"PipelineApp");

  // Start the app
  reactNativeWin32App.Start();
}

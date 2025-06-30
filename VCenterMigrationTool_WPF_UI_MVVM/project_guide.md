# Project Guide: VCenterMigrationTool_WPF_UI

Welcome, AI agent!
This guide will help you understand, extend, and maintain the VCenterMigrationTool_WPF_UI WPF/MVVM project.

## Project Purpose

This is a WPF application for managing vCenter migrations, featuring:

*   Profile management for source/destination vCenters
*   Secure credential handling
*   PowerShell-based connectivity/operations
*   Migration and validation task management
*   Activity logging

## Project Structure

VCenterMigrationTool_WPF_UI/
│
├── Models/
│   ├── ConnectionProfile.cs
│   ├── ConnectionSettings.cs
│   ├── InventoryItemBase.cs
│   ├── DatacenterInfo.cs
│   ├── ClusterInfo.cs
│   ├── HostInfo.cs
│   ├── VMInfo.cs
│   └── (other domain models)
│
├── ViewModels/
│   ├── ConnectionSettingsViewModel.cs
│   ├── MigrationViewModel.cs
│   ├── ValidationViewModel.cs
│   ├── LogViewModel.cs
│   └── (other ViewModels)
│
├── Utilities/
│   ├── ConnectionManager.cs
│   ├── PowerShellManager.cs
│   ├── LogServiceAdapter.cs
│   ├── ILogService.cs
│   └── (other helpers)
│
├── Views/
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── ConnectionSettingsWindow.xaml
│   └── ConnectionSettingsWindow.xaml.cs
│
├── Resources/
│   ├── Styles.xaml
│   └── (other resource dictionaries)
│
├── App.xaml
├── App.xaml.cs
└── project_guide.md

Raw code

## Key Patterns & Conventions

*   **MVVM:** Business/UI logic belongs in ViewModels. Views/XAML handle UI layout and bindings only.  Keep View code-behind minimal, ideally only for UI-specific tasks (like PasswordBox handling) that cannot be directly bound.
*   **DataContext:** Set in code-behind for MainWindow and major dialogs, *not* in XAML resources. This allows for proper dependency injection and testing.
*   **ViewModel Instantiation:** ViewModels are created in code-behind (often using dependency injection) and assigned to the `DataContext` of the corresponding View. Avoid creating ViewModels directly in XAML.
*   **PasswordBox Handling:** Passwords are relayed from the View via event handlers, *not* bound directly to the `Password` property. This is crucial for security.
*   **Profile Management:** Profiles are stored in `ConnectionProfile`, with secure password encryption/decryption handled by `ConnectionManager`.
*   **PowerShell Usage:** All PowerShell interactions are encapsulated in `PowerShellManager`. `PowerShellManager` depends on an `ILogService` implementation for logging and uses `IProgress<T>` for UI updates and `CancellationToken` for handling cancellations.
*   **Resource Dictionaries:** Shared styles (e.g., `ModernButtonStyle`) are in `Resources/Styles.xaml` and merged in `App.xaml`.
*   **Logging:** The application uses `ILogService` for logging. `LogViewModel` handles the presentation of log data. `LogServiceAdapter` adapts the `LogViewModel` to the `ILogService` interface, allowing you to display logs in the UI.  Consider using a more robust logging framework (like Serilog or NLog) for production applications.

## How To Extend

*   **Add a New ViewModel:** Derive from `ObservableObject`. Use `[ObservableProperty]` for properties that need change notifications and `[RelayCommand]` for commands.
*   **Add a New View/Dialog:** Create the XAML in `/Views`. Set the `DataContext` in code-behind to an instance of the corresponding ViewModel.
*   **Add a Command or Property:** Use the MVVM Toolkit attributes:

    ```csharp
    [RelayCommand]
    public async Task DoSomethingAsync() { ... }

    [ObservableProperty]
    private string _myProperty;
    ```
*   **Add/Update Styles:** Place in `Resources/Styles.xaml` and merge into `App.xaml`.
*   **Update Profile Logic:** Edit `ConnectionManager.cs` and the `ConnectionProfile`/`ConnectionSettings` models. Be extremely careful when modifying password handling logic.
*   **Add PowerShell Features:** Implement in `PowerShellManager.cs`. Expose new methods to ViewModels as needed. When adding new features to `PowerShellManager`, remember to:
    *   Inject `ILogService` for logging.
    *   Use `IProgress<T>` for reporting progress to the UI.
    *   Use `CancellationToken` for handling cancellations.
    *   Wrap PowerShell calls in `try...catch` blocks to handle exceptions gracefully.
*   **Update Logging:** To log messages, inject `ILogService` into your class and call the `LogMessage` method. The `LogViewModel` will handle displaying the messages.  Ensure that all significant actions are logged, including errors and warnings.

## Common Scenarios

*   **Adding a New Profile:**
    1.  Add a `ConnectionSettings` object to the `Profiles` collection in `ConnectionSettingsViewModel`.
    2.  Update the UI to bind to and edit the properties of the selected profile.
    3.  Use `ConnectionManager.SaveConnectionProfilesAsync()` to persist the changes.

*   **Encrypt/Decrypt Passwords:**
    *   Use the appropriate methods on `ConnectionManager` (e.g., when saving and loading profiles).

*   **Handling Passwords in the UI:**
    *   In XAML, use:

        ```xml
        <PasswordBox PasswordChanged="PasswordBox_PasswordChanged" />
        ```

    *   In code-behind:

        ```csharp
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConnectionSettingsViewModel viewModel)
            {
                viewModel.CurrentSettings.SourcePassword = ((PasswordBox)sender).Password; // Or SelectedProfile.SourcePassword if editing a selected profile
            }
        }
        ```

    *   **Important:** Never store passwords directly in memory for longer than necessary.  Encrypt them as soon as possible.

*   **Adding a New PowerShell Operation:**
    1.  Implement the operation in `PowerShellManager.cs`.
    2.  Expose the operation via an `async` method.
    3.  Call the method from a `RelayCommand` in a ViewModel.
    4.  Remember to:
        *   Inject `ILogService`.
        *   Use `IProgress<T>` to report progress to the UI.
        *   Use `CancellationToken` to allow the user to cancel the operation.

*   **Implementing Logging:**
    *   Inject `ILogService` into your class.
    *   Call the `LogMessage` method to write log entries.  Specify the log level (e.g., "INFO", "WARNING", "ERROR").
    *   Example:

        ```csharp
        private readonly ILogService _logService;

        public MyClass(ILogService logService)
        {
            _logService = logService;
        }

        public void MyMethod()
        {
            try
            {
                // ... your code ...
                _logService.LogMessage("Operation completed successfully.", "INFO");
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"An error occurred: {ex.Message}", "ERROR");
            }
        }
        ```

## Troubleshooting & Conventions

*   **Do NOT** instantiate static classes (e.g., `ConnectionManager`).
*   **Do NOT** bind directly to `PasswordBox.Password`.
*   **Do NOT** use both `Content` property and inner content in XAML controls.
*   **Do NOT** define ViewModels in XAML resources; instantiate in code-behind or using Dependency Injection.
*   **Do** use `x:Key` (not `Key`) for all XAML resources.
*   **Do** expose only public properties/commands you want to bind to in the UI.
*   `PowerShellManager` requires an `ILogService` instance to be passed in its constructor. Ensure this dependency is properly resolved.
*   When using `PowerShellManager`, always handle potential exceptions and report progress to the UI using `IProgress<T>`.
*   Ensure proper error handling in all `async` methods, especially when dealing with PowerShell.
*   Avoid blocking the UI thread. Use `async/await` and `Task.Run()` for long-running operations.

## For AI Chat Agents

*   **Always** check if a ViewModel, Command, or Property already exists before adding a new one.
*   **Strictly** follow MVVM: business logic in ViewModel, UI-only logic in Views/code-behind.
*   When extending models, ensure constructors and property setters allow required usage (especially for serialization/deserialization).
*   **Prefer** `async/await` for all I/O and PowerShell operations.
*   Update resource dictionaries when adding shared UI styles.
*   Be mindful of the `ILogService` dependency when working with `PowerShellManager`.
*   When adding new PowerShell operations, use `IProgress<T>` for UI updates and `CancellationToken` for handling cancellations.
*   When modifying existing code, ensure that you understand the purpose of the code and the potential impact of your changes.
*   Before implementing new features, consider the overall architecture and design of the application to ensure that your changes are consistent and maintainable.

## Next Steps & Project Tracking

This section lists prioritized actions for ongoing development and improvement. Please update this as tasks are completed or added.

### Current Top Priorities

*   **Profile Management:**
    *   [ ] Complete implementations for creating, editing, deleting, and persisting connection profiles in the UI.
    *   [ ] Enable robust import/export for profile data.
*   **PowerShell Integration:**
    *   [ ] Finalize migration/validation operations in `PowerShellManager`.
    *   [ ] Add comprehensive error handling and reporting for PowerShell task failures. Provide user-friendly error messages.
    *   [ ] Optimize `async` operation handling and UI progress feedback. Ensure all new `PowerShellManager` methods utilize `IProgress<T>` and `CancellationToken`. Review existing methods for compliance.
*   **Migration & Validation Workflows:**
    *   [ ] Build out the full migration pipeline in `MigrationViewModel`.
    *   [ ] Implement/expand validation tests and reporting in `ValidationViewModel`.
    *   [ ] Ensure logs capture all major user and system actions, including successes, failures, and warnings.
*   **Security Enhancements:**
    *   [ ] Review and improve password encryption/decryption methods. Consider using a more robust encryption algorithm.
    *   [ ] Audit for secure password handling in memory and on disk. Minimize the time passwords are stored in memory.
*   **User Experience:**
    *   [ ] Refine styles for accessibility and clarity (`ModernButtonStyle`, etc.).
    *   [ ] Add helpful tooltips and inline validation to forms.
    *   [ ] Ensure dialogs and error messages are clear and actionable.
*   **Codebase Maintenance:**
    *   [ ] Add XML documentation and code comments to public members.
    *   [ ] Increase test coverage on critical logic (`ConnectionManager`, `PowerShellManager`).
    *   [ ] Refactor for interface-driven dependency injection where possible. Consider using dependency injection for `ILogService` in classes other than `PowerShellManager` and `LogServiceAdapter`.  This improves testability and maintainability.

### Upcoming/Backlog

*   [ ] Implement VM/cluster selection tree and drag-and-drop migration planning.
*   [ ] Add analytics for operation success/failure statistics.
*   [ ] Support multi-language (localization/globalization) features.
*   [ ] Package the app for reliable deployment (installer/bundling).

## How to Contribute or Add Tasks

*   Add new tasks as `- [ ]` items under the appropriate section.
*   For each completed task, replace with `- [x]`.
*   If tasks require further discussion, add a `> NOTE:` line beneath.
*   For every significant change, update this section and perform a code review.

## Recent Activity

*   Refactored XAML to remove duplicate `Content` errors.
*   Resource dictionary for button styles added and referenced from `App.xaml`.
*   ViewModel instantiation and password relay pattern established in all dialogs.
*   Refactored `PowerShellManager` to use `ILogService` and `LogServiceAdapter` for logging.
*   Updated `PowerShellManager` to use `IProgress<T>` and `CancellationToken` in relevant methods.
*   Improved error handling in `PowerShellManager` methods.

Please check and update this section regularly to keep project progress visible and focused!
Key improvements and changes:

More Emphasis on Security: Added more specific guidance on password handling, including minimizing the time passwords are in memory and considering stronger encryption algorithms.
Improved Error Handling: Stressed the importance of comprehensive error handling in PowerShellManager and async methods.
Dependency Injection: Encouraged the use of dependency injection for ILogService throughout the application, not just in PowerShellManager.
Clearer ViewModel Instantiation: Emphasized creating ViewModels in code-behind and avoiding XAML instantiation.
More Specific "How To Extend" Guidance: Provided more concrete examples and best practices for adding new features.
Updated "Troubleshooting & Conventions": Added more specific "Do" and "Do NOT" items.
Improved "For AI Chat Agents" Section: Made the instructions more actionable and specific to the context of using an AI agent for development.
Enhanced Project Tracking: Added more detail to the "Current Top Priorities" section.
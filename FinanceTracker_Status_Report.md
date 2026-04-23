# FinanceTracker Project Documentation (v1.0)

## 1. Project Overview & Environment
* **Framework:** ASP.NET Core MVC (C#) [cite: User Summary].
* **Database:** SQLite using Entity Framework Core [cite: User Summary].
* **Local Environment:** Development on MSI Modern 14 (i3-1215U, 8GB RAM) [cite: User Summary].
* **Deployment Strategy:** Standalone/Portable EXE. The database location is now repaired to stay within the application folder using `AppDomain.CurrentDomain.BaseDirectory` instead of a central AppData folder [cite: Program.cs code].

## 2. Core Architecture Changes (The "Repair")
* **Database Isolation:** Removed `SpecialFolder.LocalApplicationData` dependency. Every build now creates and uses its own `FinanceTracker.db` locally in its own folder [cite: Program.cs code].
* **Single Instance Logic:** Implemented a `Mutex` to prevent multiple copies from running. If a second instance is opened, it automatically redirects to the existing local web server (Port 5000) [cite: Program.cs code].
* **Port Management:** Fixed Kestrel to listen on `127.0.0.1:5000` for consistent browser launching and single-instance checks [cite: Program.cs code].

## 3. Database Schema (FinanceDbContext)
### Transactions
* **Id:** Primary Key.
* **Title:** User-defined string [cite: Edit.cshtml].
* **Amount:** Float/Decimal value [cite: HomeController.cs].
* **Type:** "Gain" or "Loss" [cite: Edit.cshtml].
* **Category:** String matching a Name in the Category table [cite: Edit.cshtml].
* **Source:** "Cash", "Bank", "Wallet", or "Investment" [cite: HomeController.cs].
* **TransactionDate:** Date of entry [cite: Edit.cshtml].

### Categories
* **Name:** Unique string used as the identifier [cite: HomeController.cs].
* **Type:** "Gain" or "Loss" [cite: HomeController.cs].
* **Icon:** Emoji character (e.g., 🍕, 💰) [cite: HomeController.cs].

### Additional Tables
* **SpendingSummaries:** Tracks summarized spending data for reporting purposes.
* **ApplicationSettings:** Stores application-level settings and preferences.
* **TodoItems:** Represents tasks or reminders related to financial activities.

## 4. Feature Progress Status
* **[DONE] Dynamic Category Dropdowns:** Edit and Create pages fetch categories directly from the DB [cite: User Summary].
* **[DONE] Category Filtering:** JavaScript in `Edit.cshtml` filters category options based on the "Type" (Gain/Loss) selected [cite: Edit.cshtml].
* **[DONE] Navigation Persistence:** Implemented `returnUrl` logic so that "Update" and "Cancel" return the user to their specific starting page (Dashboard vs History) [cite: HomeController.cs, Edit.cshtml].
* **[DONE] CSV Handling:** Import/Export functionality with duplicate detection [cite: HomeController.cs].
* **[DONE] Timeframe Filtering:** Added logic in `HomeController` to filter transactions by daily, weekly, monthly, and custom timeframes.
* **[DONE] Category Management:** Implemented `CategoriesController` for creating, deleting, and managing categories. Includes orphan transaction handling by reassigning to "Other."
* **[IN PROGRESS] Digital Assets:** Logic for tracking "Wallets" (PhonePe/Amazon Pay) and "Investments" (Stocks/FDs) is being integrated into the `Source` logic.
* **[IN PROGRESS] Razor Pages Integration:** Transitioning from MVC to Razor Pages for better modularity and maintainability.

## 5. Known Constraints for Future AI Reference
* **Case Sensitivity:** Category name matching is currently case-insensitive due to `.ToLower()` usage in controllers [cite: HomeController.cs].
* **Unique Names:** Multiple categories should not share the exact same Name string to avoid filtering conflicts.
* **DB File:** The `FinanceTracker.db` must be present in the same folder as the `.exe` for the build to function independently.
* **Default Icon:** The `Category` model assigns a default icon ("📦") if none is provided.
* **Orphan Transactions:** Deleting a category reassigns its transactions to "Other." Ensure "Other" exists in the database.

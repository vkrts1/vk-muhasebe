# Fix Report: BankaDetay Compilation & Logic Errors

## Issue 1: CariKart Balance Update Logic
**Symptom:** `CS0200: Property or indexer 'CariKart.Bakiye' cannot be assigned to -- it is read only`
**Cause:** `CariKart.Bakiye` is a computed property (`Borc - Alacak`). The code in `BankaDetay.razor` attempted to modify it directly.
**Fix:** Removed the manual update logic in `BankaDetay.razor` because `DatabaseService.SaveCariHareketAsync` **already** updates the `CariKart` balance (`Borc` and `Alacak`) inside a transaction. This prevented double-counting and fixed the compilation error.

## Issue 2: BankaDetayView Accessing ViewModel Event
**Symptom:** `CS0070: 'BankaDetayViewModel.RequestFilePick' event can only appear on the left hand side of += or -=`
**Cause:** `RequestFilePick` was declared as an `event`, but the View (`BankaDetayView.axaml.cs`) was trying to assign a delegate to it (`=`).
**Fix:** Changed `RequestFilePick` in `BankaDetayViewModel.cs` to a `public Func<Task<string?>>? RequestFilePick { get; set; }` property.

## Issue 3: Namespace Ambiguity
**Symptom:** `CS0234: The type or namespace name 'Platform' does not exist in the namespace 'ErmayMuhasebe.Avalonia'`
**Cause:** `Avalonia.Platform` was being hidden by the root namespace `ErmayMuhasebe.Avalonia`.
**Fix:** Used `global::Avalonia.Platform...` in `BankaDetayView.axaml.cs` to explicitly reference the Avalonia assembly namespace.

## Result
Both `ErmayMuhasebe.Shared` and `ErmayMuhasebe.Avalonia` now build successfully.

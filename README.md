# GM.XrmToolBox.UnifiedTranslatorManager

**Unified Translator Manager** is an [XrmToolBox](https://www.xrmtoolbox.com/) plugin that lets you manage **Dataverse / Dynamics 365 metadata translations** (tables, forms, fields, choices, views, business process flows, …) from a single grid.

It supports two complementary workflows:

1. **In-grid editing** – edit each target cell directly, save and publish from the toolbar.
2. **Excel round-trip** – export the visible rows to an `.xlsx` file, hand it to a translator, then re-import the edited file. Modified rows are highlighted in yellow.

The plugin targets **.NET Framework 4.8** and runs inside any recent XrmToolBox host.

---

## Features

- Loads translation rows for a connected Dataverse / Dynamics 365 environment.
- Scope selector: **Table**, **Form**, **Choices**, plus combinations.
- Solution-scoped loading (strongly recommended) or full unmanaged-metadata scan.
- Per-row source / target view with inline editing of the target column.
- Auto-managed **Modified** flag and yellow highlight for changed cells.
- **Save changes** writes back to Dataverse; **Publish changed** publishes only the touched entities; **Publish all** publishes the whole solution.
- **Excel export / import** (`.xlsx`) with no third-party dependencies — works without ClosedXML or the OpenXML SDK, so it does not collide with assemblies preloaded by the XrmToolBox host.
- Built-in **Help** dialog with step-by-step instructions for both workflows.

---

## Installation

### From the XrmToolBox Plugin Store
Once the plugin is published to the Plugin Store, install it directly from the **Configuration → Plugins Store** page in XrmToolBox.

### Manual install
1. Build the project in **Release** configuration.
2. Copy the resulting `GM.XrmToolBox.UnifiedTranslatorManager.dll` (and the satellite `Plugins\` folder if any) into the XrmToolBox `Plugins` folder.
3. Restart XrmToolBox. The plugin appears as **Universal Translation Manager**.

---

## Quick start

### Single-record translation (edit in the grid)

1. Click **Load context** to fetch solutions, languages and entities from the connected environment.
2. Pick the **Mode** (suggested: Table + Form + Choices).
3. Pick the **Solution** that contains the components to translate. *Always pick a specific solution before loading the session.*
4. Pick the **Source** and **Target** languages.
5. Click **Load session** to fill the grid.
6. Use the search textbox / filter checkboxes to find the row(s) to translate.
7. Click the **Target** cell once — it enters edit mode immediately. Type the new value (an empty value is allowed). When you leave the cell the row turns yellow and **Modified** is ticked automatically.
8. Click **Save changes** and then **Publish changed** (or **Publish all**).

### Bulk translation via Excel

1. Click **Load context**.
2. Pick **Mode**, **Solution** and **Source / Target language**.
3. Click **Load session**.
4. Apply any filter so the grid contains only the rows you want to translate.
5. In the *Export / Import from Excel file* section click **Export to Excel** and save the `.xlsx` file.
6. Open the file in Excel. Edit only the **Target** column. Do **not** rename or remove the *Entity*, *Key* and *Property* columns — they are used to match rows on import. Save the file in `.xlsx` format.
7. Click **Import from Excel** and select the saved file. Updated rows turn yellow.
8. Review the import summary (changed / unchanged / not found / ambiguous rows).
9. Click **Save changes** and then **Publish changed**.

> ⚠️ If you click **Load session** without picking a specific solution, the tool will scan **all unmanaged metadata** of the environment. This can take from several minutes up to an hour or more on large tenants. The plugin will warn you and ask for explicit confirmation before continuing.

---

## Tips

- **Yellow target cells** = pending modifications not yet saved to Dataverse.
- The **Modified** column is read-only: it ticks automatically when the target value differs from the original. Re-typing the original value un-ticks it.
- You can repeat the export/import cycle as many times as needed before saving.
- Always work against an unmanaged solution containing only the components you actually want to translate.

---

## Building from source

Prerequisites:

- Visual Studio 2022 / 2026 with the .NET desktop development workload.
- .NET Framework 4.8 developer pack.

Steps:

```powershell
git clone https://github.com/<your-account>/GM.XrmToolBox.UnifiedTranslatorManager.git
cd GM.XrmToolBox.UnifiedTranslatorManager
msbuild GM.XrmToolBox.UnifiedTranslatorManager.sln /t:Restore;Build /p:Configuration=Release
```

The build copies the plugin assembly into the XrmToolBox `Plugins\` folder when the appropriate post-build target is configured.

---

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `Could not load file or assembly 'System.Memory, Version=4.0.1.1...'` when exporting / importing | This is the historic ClosedXML conflict inside XrmToolBox. The current build no longer uses ClosedXML; rebuild from latest source and reinstall the plugin. |
| Loading the session takes forever | Make sure you picked a **specific** solution before clicking *Load session*. The default *(All)* entry forces a scan of every unmanaged component. |
| Cell does not enter edit mode on the first click | Make sure you are clicking the **Target** column (it is the only editable column). Other columns are read-only by design. |
| Imported rows show up as *Not found* | Verify that the Excel file still contains the original *Entity*, *Key* and *Property* columns and that you did not rename them. These three columns drive the row matching. |

---

## License

This project is released under the MIT License — see [License.txt](License.txt).

---

## Acknowledgements

- [XrmToolBox](https://www.xrmtoolbox.com/) and its plugin SDK.
- Microsoft Dataverse / Dynamics 365 SDK assemblies.

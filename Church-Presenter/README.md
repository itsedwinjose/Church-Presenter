# Church Presenter

Offline WPF worship-service presentation software. During development, all application data is stored directly in the project root as `church-presenter.db`. Published copies store it beside the executable, so the deployment remains self-contained and can be backed up by copying that folder.

## Bible import

Use **Bible Library → Import CSV Bible**. The file must be UTF-8 and have this exact header:

```csv
Book,Testament,Chapter,Verse,Text
ஆதியாகமம்,Old,1,1,ஆதியிலே தேவன் வானத்தையும் பூமியையும் சிருஷ்டித்தார்.
```

The importer stores each verse in the hierarchy `Book → Chapter → Verse`, replaces duplicate references safely, and indexes verse text for offline search. Supply a Tamil Bible file that your church is licensed to use; no copyrighted Bible text is bundled with the app.

## Build and package

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

Install [Inno Setup](https://jrsoftware.org/isinfo.php), then compile `Installer\ChurchPresenter.iss`. The resulting `ChurchPresenterSetup.exe` is created in `Installer\Output`.

## Display controls

- Set the fullscreen background and font colors with hex values in **Settings**.
- Change the presentation type size with the slider.
- Select a Bible verse and choose **Present selected verse**.
- In the presentation window: `Esc` closes it and `B` blacks the screen.

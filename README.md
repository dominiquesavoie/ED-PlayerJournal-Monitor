# ED-PlayerJournal-Monitor
A small VoiceAttack plugin that reads the contents of the player journal and sends it to Voice attack.


## Usage

To have the Journal Monitor send an event and it's revelent data to VoiceAttack, you need to configure a command for the event.

The command must be in the following pattern:
`((EDPlayerJournal_{EventName}))`

For example, if you want to listen to ScanOrganics, you need to set the following command:
`((EDPlayerJournal_ScanOrganic))`

The data will then be feeded to VoiceAttack using variables with the pattern `EDPlayerJournal_{EventName}_{EventField}`
For exmaple, with Scan Organic you can access the ScanType with `EDPlayerJournal_ScanOrganic_ScanType` and the name of the species with `EDPlayerJournal_ScanOrganic_Genus_Localised`.

// Put an icon and a name into a Windows exe without Windows (pure JS, uses the `resedit` package).
// node set_exe_icon.mjs <in.exe> <icon.ico> <out.exe> <Name>      (build_windows.sh calls this)
import * as ResEdit from "resedit";
import fs from "node:fs";

const [, , input, icon, output, name] = process.argv;
const exe = ResEdit.NtExecutable.from(fs.readFileSync(input));
const res = ResEdit.NtExecutableResource.from(exe);
const iconFile = ResEdit.Data.IconFile.from(fs.readFileSync(icon));
const groups = ResEdit.Resource.IconGroupEntry.fromEntries(res.entries);
if (groups.length === 0) throw new Error("no icon group in " + input);
for (const group of groups) {
  ResEdit.Resource.IconGroupEntry.replaceIconsForResource(
    res.entries, group.id, group.lang, iconFile.icons.map((item) => item.data));
}
// the name shown by Task Manager and the file properties
for (const info of ResEdit.Resource.VersionInfo.fromEntries(res.entries)) {
  for (const lang of info.getAllLanguagesForStringValues()) {
    info.setStringValues(lang, {FileDescription: name, ProductName: name, InternalName: name, OriginalFilename: name + ".exe"});
  }
  info.outputToResourceEntries(res.entries);
}
res.outputResource(exe);
fs.writeFileSync(output, Buffer.from(exe.generate()));

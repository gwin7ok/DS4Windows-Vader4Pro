import sys
import os
import xml.etree.ElementTree as ET
from xml.dom import minidom

# Usage: python convert_profile_to_old.py <input_profiles.xml> <output_folder>
# Best-effort converter: looks for known element names anywhere in source XML
# and copies values into legacy 3.9.9-style <Profile> output.

KNOWN_ELEMENTS = [
    "useExclusiveMode","startMinimized","minimizeToTaskbar",
    "formWidth","formHeight","formLocationX","formLocationY",
    "LastChecked","CheckWhen","LastVersionChecked",
    "Notifications","DisconnectBTAtStop","SwipeProfiles","QuickCharge",
    "CloseMinimizes","UseLang","DownloadLang","FlashWhenLate","FlashWhenLateAt",
    "AppIcon","AppTheme","UseOSCServer","OSCServerPort","InterpretingOscMonitoring",
    "UseOSCSender","OSCSenderPort","OSCSenderAddress",
    "UseUDPServer","UDPServerPort","UDPServerListenAddress",
    "UseCustomSteamFolder","CustomSteamFolder","AutoProfileRevertDefaultProfile",
    "AbsRegionDisplay"
]

def find_any(root, name):
    # find first element with local-name == name anywhere
    for elem in root.iter():
        if elem.tag.endswith('}' + name) or elem.tag == name:
            return elem
    return None


def add_element(parent, name, text):
    el = ET.Element(name)
    el.text = text
    parent.append(el)


def prettify(elem):
    rough_string = ET.tostring(elem, 'utf-8')
    reparsed = minidom.parseString(rough_string)
    return reparsed.toprettyxml(indent="  ")


def main():
    if len(sys.argv) < 3:
        print("Usage: python convert_profile_to_old.py <input_profiles.xml> <output_folder>")
        sys.exit(1)

    src = sys.argv[1]
    outdir = sys.argv[2]
    if not os.path.isfile(src):
        print(f"Input file not found: {src}")
        sys.exit(2)

    if not os.path.isdir(outdir):
        os.makedirs(outdir, exist_ok=True)

    tree = ET.parse(src)
    root = tree.getroot()

    # create output root
    profile = ET.Element('Profile')
    # try to copy app_version and config_version if present on source root
    try:
        appver = root.attrib.get('app_version') or root.attrib.get('version')
        if appver:
            profile.set('app_version', appver)
    except Exception:
        pass

    # Attempt to copy known simple elements
    for name in KNOWN_ELEMENTS:
        node = find_any(root, name)
        if node is not None and node.text is not None:
            add_element(profile, name, node.text)

    # Controllers (Controller1..Controller4)
    for i in range(1,5):
        # look for Controller elements or Controller{i} tags
        node = find_any(root, f'Controller{i}')
        if node is None:
            node = find_any(root, 'Controller')
        if node is not None and node.text:
            add_element(profile, f'Controller{i}', node.text)

    # CustomLed entries (CustomLed1..CustomLed4)
    for i in range(1,5):
        name = f'CustomLed{i}'
        node = find_any(root, name)
        if node is not None and node.text:
            add_element(profile, name, node.text)

    # DeviceOptions and nested Enabled fields
    deviceOptionsNode = None
    # try to find a DeviceOptions node
    for elem in root.iter():
        tag = elem.tag
        if tag.endswith('DeviceOptions') or tag == 'DeviceOptions':
            deviceOptionsNode = elem
            break
    if deviceOptionsNode is not None:
        # copy simple Enabled flags if present
        for child in deviceOptionsNode:
            # child might be DS4SupportSettings etc.
            enabled = None
            enode = None
            for c in child:
                if c.tag.endswith('Enabled') or c.tag == 'Enabled':
                    enode = c
                    break
            if enode is not None and enode.text is not None:
                # create container element with same name
                newChild = ET.Element(child.tag)
                newEnabled = ET.Element('Enabled')
                newEnabled.text = enode.text
                newChild.append(newEnabled)
                profile.append(newChild)

    # Write out file
    outpath = os.path.join(outdir, 'Profiles_3.9.9_compatible.xml')
    xmlstr = prettify(profile)
    with open(outpath, 'w', encoding='utf-8') as f:
        f.write(xmlstr)

    print(f"Converted file written to: {outpath}")

if __name__ == '__main__':
    main()

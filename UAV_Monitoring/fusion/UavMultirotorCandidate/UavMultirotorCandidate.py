"""Fusion script: multirotor candidate for the UAV airframe trade (WBS 1.1 and 1.2.1).

Run from Fusion: Utilities > Scripts and Add-Ins > Scripts > + > choose this folder > Run.

Creates one new design document, "UAV - Assembly", containing
    UAV Airframe       centre plate                         WBS 1.1.1
    UAV Motor Arm      one component, four occurrences       WBS 1.1.1
    UAV Motor Mount    one component, four occurrences       WBS 1.1.2
    UAV Payload Tray   under the plate                       WBS 1.2.1
driven by user parameters, then
    writes  uav_mass_properties.json  (mass, centre of mass, inertia per component)
    exports UAV_Assembly.step and UAV_Assembly.stl
    saves the design into the active Fusion project (if you agree)
    starts Fusion's Create Drawing workflow on the saved design.

The drawing step is started, not finished, by the script. Fusion has no API
for drawing views, so you place the base view and save the drawing as
"UAV Airframe Drawing". PD Quick starts drawings the same way.

Dimensions are a provisional candidate for comparison against the VTOL
fixed-wing. They are not a sized or analysed airframe.

Not run before delivery. Lengths are in mm in PARAMETERS; the API works in cm.
"""
import json
import math
import os
import traceback

import adsk.core
import adsk.fusion

# name: (value in mm, comment)
PARAMETERS = {
    "plate_size": (160.0, "Centre plate side"),
    "plate_thickness": (3.0, "Centre plate thickness"),
    "arm_length": (220.0, "Arm length from plate centre to motor axis"),
    "arm_width": (20.0, "Arm width"),
    "arm_height": (12.0, "Arm height"),
    "mount_diameter": (36.0, "Motor mount disc diameter"),
    "mount_thickness": (4.0, "Motor mount disc thickness"),
    "tray_length": (110.0, "Payload tray length"),
    "tray_width": (70.0, "Payload tray width"),
    "tray_thickness": (2.5, "Payload tray thickness"),
    "tray_drop": (40.0, "Payload tray distance below the plate"),
}
WBS = {"UAV Airframe": "1.1.1", "UAV Motor Arm": "1.1.1", "UAV Motor Mount": "1.1.2", "UAV Payload Tray": "1.2.1"}
MATERIALS = ("Aluminum 6061", "Aluminum")
LIBRARIES = ("Fusion Material Library", "Fusion 360 Material Library")


def mm(value):
    return value / 10.0


def find_material(app):
    for library_name in LIBRARIES:
        library = app.materialLibraries.itemByName(library_name)
        if library is None:
            continue
        for name in MATERIALS:
            material = library.materials.itemByName(name)
            if material is not None:
                return material
    return None


def box(component, name, length, width, height, z_offset):
    """Rectangular body centred on the origin in X and Y, from z_offset upward. Sizes in mm."""
    sketch = component.sketches.add(component.xYConstructionPlane)
    sketch.name = name + " profile"
    sketch.sketchCurves.sketchLines.addTwoPointRectangle(
        adsk.core.Point3D.create(-mm(length) / 2, -mm(width) / 2, 0),
        adsk.core.Point3D.create(mm(length) / 2, mm(width) / 2, 0))
    extrudes = component.features.extrudeFeatures
    feature_input = extrudes.createInput(sketch.profiles.item(0), adsk.fusion.FeatureOperations.NewBodyFeatureOperation)
    start = adsk.fusion.OffsetStartDefinition.create(adsk.core.ValueInput.createByReal(mm(z_offset)))
    feature_input.startExtent = start
    feature_input.setDistanceExtent(False, adsk.core.ValueInput.createByReal(mm(height)))
    feature = extrudes.add(feature_input)
    feature.bodies.item(0).name = name
    return feature


def disc(component, name, diameter, height, z_offset):
    sketch = component.sketches.add(component.xYConstructionPlane)
    sketch.name = name + " profile"
    sketch.sketchCurves.sketchCircles.addByCenterRadius(adsk.core.Point3D.create(0, 0, 0), mm(diameter) / 2)
    extrudes = component.features.extrudeFeatures
    feature_input = extrudes.createInput(sketch.profiles.item(0), adsk.fusion.FeatureOperations.NewBodyFeatureOperation)
    feature_input.startExtent = adsk.fusion.OffsetStartDefinition.create(adsk.core.ValueInput.createByReal(mm(z_offset)))
    feature_input.setDistanceExtent(False, adsk.core.ValueInput.createByReal(mm(height)))
    feature = extrudes.add(feature_input)
    feature.bodies.item(0).name = name
    return feature


def placement(angle_deg, radius_mm):
    """Rotation about Z by angle, then translation to radius along the rotated X axis."""
    matrix = adsk.core.Matrix3D.create()
    matrix.setToRotation(math.radians(angle_deg), adsk.core.Vector3D.create(0, 0, 1), adsk.core.Point3D.create(0, 0, 0))
    matrix.translation = adsk.core.Vector3D.create(
        mm(radius_mm) * math.cos(math.radians(angle_deg)), mm(radius_mm) * math.sin(math.radians(angle_deg)), 0)
    return matrix


def properties(component, count):
    p = component.getPhysicalProperties(adsk.fusion.CalculationAccuracy.HighCalculationAccuracy)
    ok, xx, yy, zz, xy, yz, xz = p.getXYZMomentsOfInertia()
    com = p.centerOfMass
    return {
        "wbs": WBS.get(component.name, "1.1"),
        "occurrences": count,
        "material": component.material.name if component.material else None,
        "mass_kg": p.mass,
        "volume_cm3": p.volume,
        "centre_of_mass_cm": [com.x, com.y, com.z],
        "inertia_about_origin_kg_cm2": {"xx": xx, "yy": yy, "zz": zz, "xy": xy, "yz": yz, "xz": xz} if ok else None,
    }


def run(context):
    app = adsk.core.Application.get()
    ui = app.userInterface
    try:
        folder_dialog = ui.createFolderDialog()
        folder_dialog.title = "Folder for the mass properties, STEP and STL files"
        if folder_dialog.showDialog() != adsk.core.DialogResults.DialogOK:
            return
        out = folder_dialog.folder

        document = app.documents.add(adsk.core.DocumentTypes.FusionDesignDocumentType)
        design = adsk.fusion.Design.cast(app.activeProduct)
        design.designType = adsk.fusion.DesignTypes.ParametricDesignType
        root = design.rootComponent
        root.name = "UAV - Assembly"
        for name, (value, comment) in PARAMETERS.items():
            design.userParameters.add(name, adsk.core.ValueInput.createByString("%g mm" % value), "mm", comment)
        v = {name: value for name, (value, _) in PARAMETERS.items()}
        material = find_material(app)
        identity = adsk.core.Matrix3D.create()

        def new_component(name, transform):
            occurrence = root.occurrences.addNewComponent(transform)
            occurrence.component.name = name
            occurrence.component.description = "WBS " + WBS[name]
            if material is not None:
                occurrence.component.material = material
            return occurrence

        airframe = new_component("UAV Airframe", identity)
        box(airframe.component, "Centre plate", v["plate_size"], v["plate_size"], v["plate_thickness"], 0)

        # Arm: modelled along +X from the plate edge to the motor axis, then placed at 45, 135, 225, 315 degrees.
        inner = v["plate_size"] / 2 - 10.0
        length = v["arm_length"] - inner
        arm_centre = inner + length / 2
        angles = (45, 135, 225, 315)
        arm = new_component("UAV Motor Arm", placement(angles[0], arm_centre))
        box(arm.component, "Arm", length, v["arm_width"], v["arm_height"], v["plate_thickness"])
        mount = new_component("UAV Motor Mount", placement(angles[0], v["arm_length"]))
        disc(mount.component, "Mount disc", v["mount_diameter"], v["mount_thickness"], v["plate_thickness"] + v["arm_height"])
        for angle in angles[1:]:
            root.occurrences.addExistingComponent(arm.component, placement(angle, arm_centre))
            root.occurrences.addExistingComponent(mount.component, placement(angle, v["arm_length"]))

        tray = new_component("UAV Payload Tray", identity)
        box(tray.component, "Tray", v["tray_length"], v["tray_width"], v["tray_thickness"], -v["tray_drop"])
        if design.snapshots.hasPendingSnapshot:
            design.snapshots.add()

        report = {
            "status": "Provisional multirotor candidate; not a sized or analysed airframe",
            "units": {"mass": "kg", "length": "cm", "inertia": "kg*cm^2"},
            "material_requested": MATERIALS[0],
            "material_found": material.name if material is not None else None,
            "parameters_mm": v,
            "components": {
                "UAV Airframe": properties(airframe.component, 1),
                "UAV Motor Arm": properties(arm.component, 4),
                "UAV Motor Mount": properties(mount.component, 4),
                "UAV Payload Tray": properties(tray.component, 1),
            },
            "assembly": properties(root, 1),
        }
        report["assembly"]["note"] = "Structure only: no motors, battery, electronics or payload mass."
        with open(os.path.join(out, "uav_mass_properties.json"), "w", encoding="utf-8") as handle:
            json.dump(report, handle, indent=2)

        exports = design.exportManager
        exports.execute(exports.createSTEPExportOptions(os.path.join(out, "UAV_Assembly.step"), root))
        stl = exports.createSTLExportOptions(root, os.path.join(out, "UAV_Assembly.stl"))
        stl.meshRefinement = adsk.fusion.MeshRefinementSettings.MeshRefinementMedium
        exports.execute(stl)

        message = ("Created UAV - Assembly with %d occurrences.\nStructure mass %.3f kg (%s).\nFiles are in %s."
                   % (root.occurrences.count, report["assembly"]["mass_kg"], report["material_found"] or "default material", out))
        project = app.data.activeProject
        if project is None:
            ui.messageBox(message + "\n\nNo active Fusion project, so the design was not saved. Save it, then use "
                          "File > New Drawing > From Design.")
            return
        answer = ui.messageBox(message + "\n\nSave as 'UAV - Assembly' in project '%s' and start the drawing workflow?" % project.name,
                               "UAV multirotor candidate", adsk.core.MessageBoxButtonTypes.YesNoButtonType)
        if answer != adsk.core.DialogResults.DialogYes:
            return
        if not document.saveAs("UAV - Assembly", project.rootFolder, "Multirotor candidate, WBS 1.1. Provisional.", ""):
            ui.messageBox("Fusion did not save the design. Save it by hand, then use File > New Drawing > From Design.")
            return
        adsk.doEvents()
        command = ui.commandDefinitions.itemById("NewFusionDrawingDocumentCommand")
        if command is None or not command.execute():
            ui.messageBox("The drawing command is not available in this Fusion version. Use File > New Drawing > From Design.")
    except Exception:
        ui.messageBox("UAV multirotor candidate failed:\n" + traceback.format_exc())

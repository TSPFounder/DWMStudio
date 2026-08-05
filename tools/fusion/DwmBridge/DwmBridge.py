# -*- coding: utf-8 -*-
"""DwmBridge — a localhost HTTP bridge into Fusion, for DWMStudio.

Fusion ships no COM automation. Its API is Python running INSIDE Fusion, so nothing
outside the process can reach in. This add-in is the missing half: it listens on
127.0.0.1 and turns requests into Fusion API calls.

It INVOKES WindTurbineBlade.py rather than reimplementing it. That script already
builds the rotor from the BOM and has 26 geometry checks against it; duplicating any
of that here would guarantee the two drift.

INSTALL
-------
Copy this folder to:

    Windows  %APPDATA%\\Autodesk\\Autodesk Fusion 360\\API\\AddIns\\DwmBridge\\
    macOS    ~/Library/Application Support/Autodesk/Autodesk Fusion 360/API/AddIns/DwmBridge/

Fusion requires the folder name and the .py name to match. Then:
Utilities -> ADD-INS -> Scripts and Add-Ins -> Add-Ins tab -> DwmBridge -> Run,
and tick "Run on Startup".

THE ONE THING THAT MAKES THIS HARD
----------------------------------
THE FUSION API IS NOT THREAD-SAFE, AND AN HTTP HANDLER IS ON THE WRONG THREAD.

Touching adsk.* from the socket thread corrupts Fusion's state. It usually does not
crash immediately, which is worse than if it did -- you get a Fusion that misbehaves
minutes later with no connection to the cause.

So every command here is marshalled onto Fusion's main thread with a CUSTOM EVENT:
the HTTP thread files a job, fires the event, and blocks on a threading.Event until
the main thread has run it. Nothing below the marshalling boundary is allowed to
touch adsk.*, and nothing above it is allowed to do anything else.

Two consequences worth knowing:
  * If Fusion is showing a modal dialog, the main thread is not pumping events and
    every request times out. WindTurbineBlade.run() ends in ui.messageBox, which is
    why this add-in calls its BUILDERS instead of its run().
  * Handler objects must be kept alive in a module-level list. A locally-scoped
    Fusion event handler is garbage collected and stops firing, silently.

SECURITY
--------
Binds 127.0.0.1 only, never 0.0.0.0. This executes CAD operations and writes files
on request; it must not be reachable from the network.
"""

import json
import os
import sys
import threading
import traceback
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import adsk.core
import adsk.fusion


# =============================================================================
# CONFIG
# =============================================================================

HOST = '127.0.0.1'
PORT = 18750

# Where WindTurbineBlade.py might live. Tried in order; the first hit wins.
# Deliberately NOT hardcoded to one developer's layout -- override with the
# DWM_FUSION_SCRIPTS environment variable.
SCRIPT_SEARCH_PATHS = [
    os.environ.get('DWM_FUSION_SCRIPTS', ''),
    r'C:\DreamWorldMaker\Repos\DWM_Dev\Models\Fusion\MVP_WindTurbine',
    os.path.join(os.path.dirname(__file__), '..', '..', '..',
                 'Models', 'Fusion', 'MVP_WindTurbine'),
]

# A build of the full rotor takes minutes. A request that has not answered in this
# long is a stuck main thread, not a slow loft.
JOB_TIMEOUT_S = 900

CUSTOM_EVENT_ID = 'DwmBridgeJob'

_app = None
_ui = None
_server = None
_server_thread = None
_event_handler = None
_custom_event = None

# HANDLERS MUST BE KEPT ALIVE HERE. A Fusion event handler held only by a local
# variable is collected and stops firing, with no error.
_handlers = []

_jobs = {}
_jobs_lock = threading.Lock()


# =============================================================================
# MAIN-THREAD MARSHALLING
# =============================================================================

class _Job(object):
    def __init__(self, fn):
        self.fn = fn
        self.done = threading.Event()
        self.result = None
        self.error = None


class _JobHandler(adsk.core.CustomEventHandler):
    """Runs one filed job ON FUSION'S MAIN THREAD. The only place adsk.* is touched."""

    def notify(self, args):
        job_id = args.additionalInfo
        with _jobs_lock:
            job = _jobs.pop(job_id, None)
        if job is None:
            return
        try:
            job.result = job.fn()
        except Exception:
            # Captured, never raised: an exception escaping a Fusion event handler
            # takes down more than this request.
            job.error = traceback.format_exc()
        finally:
            job.done.set()


def _on_main(fn):
    """File a callable for the main thread and wait for it. Called from HTTP threads."""
    job = _Job(fn)
    job_id = uuid.uuid4().hex

    with _jobs_lock:
        _jobs[job_id] = job

    _app.fireCustomEvent(CUSTOM_EVENT_ID, job_id)

    if not job.done.wait(JOB_TIMEOUT_S):
        with _jobs_lock:
            _jobs.pop(job_id, None)
        raise RuntimeError(
            'Fusion did not run the command within %d s.\n\n'
            'The usual cause is a MODAL DIALOG holding the main thread -- Fusion '
            'cannot process events while one is open, so every request queues behind '
            'it. Check Fusion for an open message box.' % JOB_TIMEOUT_S)

    if job.error:
        raise RuntimeError(job.error)
    return job.result


# =============================================================================
# THE BUILD SCRIPT
# =============================================================================

def _import_blade_script():
    """Import WindTurbineBlade, or explain precisely where it was looked for.

    NOT imported at add-in load. The bridge has to come up even when the script is
    missing, so that /ping and /massProperties still answer and the failure is
    reported per-request instead of as an add-in that will not start.
    """
    tried = []
    for path in SCRIPT_SEARCH_PATHS:
        if not path:
            continue
        full = os.path.abspath(path)
        tried.append(full)
        if not os.path.isfile(os.path.join(full, 'WindTurbineBlade.py')):
            continue
        if full not in sys.path:
            sys.path.insert(0, full)
        import WindTurbineBlade
        return WindTurbineBlade

    raise RuntimeError(
        'WindTurbineBlade.py was not found. Looked in:\n  ' + '\n  '.join(tried) +
        '\n\nSet DWM_FUSION_SCRIPTS to the folder that contains it.')


def _design(reuse_document):
    """The design to build into.

    REUSE IS THE DEFAULT, and that is not a style choice. WindTurbineBlade.run()
    calls documents.add() every time, so ten builds leave ten open documents --
    and Fusion's free tier caps ACTIVE documents at 10. Past that cap components
    drop to Inactive (Read-Only), where physicalProperties returns 0 WITHOUT
    raising. Zero mass, no error, straight into whatever consumes it.

    A build loop reaches that cap on its own. Reusing the document is what stops
    the automation manufacturing the failure.
    """
    if reuse_document:
        design = adsk.fusion.Design.cast(_app.activeProduct)
        if design is not None:
            return design

    _app.documents.add(adsk.core.DocumentTypes.FusionDesignDocumentType)
    design = adsk.fusion.Design.cast(_app.activeProduct)
    if design is None:
        raise RuntimeError('Could not obtain a Fusion design document.')
    return design


# =============================================================================
# COMMANDS -- all run on the main thread
# =============================================================================

def _cmd_build(payload):
    """Build the rotor by calling WindTurbineBlade.build_rotor.

    ONE CALL, NO DUPLICATED ORCHESTRATION. This used to copy the script's precone
    and blade-placement block, which was fifteen lines nobody would ever diff: the
    twist convention could change in the script and silently not change here. That
    block now lives in build_rotor and both callers share it.

    Its run() is still deliberately NOT called. run() creates a document and ends
    in a modal ui.messageBox -- the two things an automated caller must not
    inherit, which is exactly why the split was worth asking for.
    """
    wtb = _import_blade_script()

    if not hasattr(wtb, 'build_rotor'):
        # FAILS LOUDLY RATHER THAN FALLING BACK. A fallback that reimplemented the
        # assembly would put the duplication straight back, and would do it in the
        # one situation where the two versions are already known to disagree.
        raise RuntimeError(
            'This WindTurbineBlade.py has no build_rotor(design, cfg, log).\n\n'
            'The bridge needs the refactored script: run(context) split into the '
            'interactive wrapper and build_rotor, so both call one implementation. '
            'Update the script rather than reverting the bridge -- copying the '
            'assembly step back in here is what the split removed.')

    cfg = dict(wtb.CONFIG)
    overrides = payload.get('config') or {}
    unknown = [k for k in overrides if k not in cfg]
    cfg.update(overrides)

    design = _design(payload.get('reuseDocument', True))

    log = []
    if unknown:
        # Reported, not rejected: CONFIG may legitimately gain keys. But a typo in
        # an override that is silently ignored would leave the caller believing it
        # changed something.
        log.append('Ignored unknown config key(s): %s' % ', '.join(sorted(unknown)))

    wtb.build_rotor(design, cfg, log)

    return {
        'log': log,
        'document': _app.activeDocument.name,
        'componentCount': design.allComponents.count,
        # Echoed back so a caller can record what was actually built rather than
        # what it believes it asked for.
        'config': {k: cfg[k] for k in sorted(cfg)},
    }


def _cmd_mass_properties(payload):
    """Mass, centre of mass and inertia for every component.

    bodyCount IS RETURNED ON PURPOSE. A component with no bodies legitimately has
    no mass; a component WITH bodies reporting zero mass is the Inactive
    (Read-Only) failure. Without the count those two are the same number, and only
    one of them is a problem.
    """
    design = adsk.fusion.Design.cast(_app.activeProduct)
    if design is None:
        raise RuntimeError(
            'No active Fusion design. The add-in reads the ACTIVE document, which '
            'is whatever has focus -- not whatever the caller has in mind.')

    out = []
    for i in range(design.allComponents.count):
        comp = design.allComponents.item(i)

        try:
            props = comp.getPhysicalProperties(
                adsk.fusion.CalculationAccuracy.HighCalculationAccuracy)
        except Exception:
            props = comp.physicalProperties

        entry = {
            'name': comp.name,
            'bodyCount': comp.bRepBodies.count,
            # The API documents mass in kilograms.
            'mass': float(props.mass),
        }

        com = props.centerOfMass
        if com is not None:
            # Fusion's internal length unit is the CENTIMETRE regardless of what
            # the document displays -- the same trap WindTurbineBlade's header
            # warns about, in the opposite direction. Converted once, here.
            entry['centreOfMass'] = [com.x / 100.0, com.y / 100.0, com.z / 100.0]

        ok, xx, yy, zz, xy, yz, xz = props.getXYZMomentsOfInertia()
        if ok:
            entry['inertia'] = [xx, yy, zz, xy, yz, xz]
            # NOT CONVERTED, AND SAID SO. Mass is documented in kg and the length
            # unit is known, but the inertia unit has not been verified against a
            # hand-checked solid. Silently applying a factor that might be wrong
            # would be worse than handing over the raw numbers with a label.
            entry['inertiaUnits'] = 'UNVERIFIED - as returned by Fusion, believed kg*cm^2'

        out.append(entry)

    return {'components': out}


def _cmd_export(payload):
    """Export the active design. Format comes from the filename's extension."""
    path = payload.get('path')
    if not path:
        raise RuntimeError("An export needs a 'path'.")

    design = adsk.fusion.Design.cast(_app.activeProduct)
    if design is None:
        raise RuntimeError('No active Fusion design to export.')

    folder = os.path.dirname(os.path.abspath(path))
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)

    mgr = design.exportManager
    ext = os.path.splitext(path)[1].lower()

    if ext in ('.step', '.stp'):
        options = mgr.createSTEPExportOptions(path, design.rootComponent)
    elif ext == '.stl':
        options = mgr.createSTLExportOptions(design.rootComponent, path)
    elif ext == '.f3d':
        options = mgr.createFusionArchiveExportOptions(path, design.rootComponent)
    elif ext == '.iges':
        options = mgr.createIGESExportOptions(path, design.rootComponent)
    else:
        raise RuntimeError(
            'No exporter for "%s". Supported: .step, .stp, .stl, .iges, .f3d' % ext)

    if not mgr.execute(options):
        raise RuntimeError('Fusion refused the export to %s.' % path)

    # VERIFIED ON DISK, not inferred from execute() returning True. The whole
    # project's recurring bug is a tool reporting success without the artifact.
    if not os.path.isfile(path):
        raise RuntimeError(
            'Fusion reported the export succeeded but no file is at %s.' % path)

    return {'path': path, 'bytes': os.path.getsize(path)}


COMMANDS = {
    'build': _cmd_build,
    'massProperties': _cmd_mass_properties,
    'export': _cmd_export,
}


# =============================================================================
# HTTP
# =============================================================================

class _Handler(BaseHTTPRequestHandler):

    protocol_version = 'HTTP/1.1'

    def log_message(self, fmt, *args):
        pass   # Fusion's Text Commands window is not a web server log

    def _send(self, status, obj):
        body = json.dumps(obj).encode('utf-8')
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if self.path.rstrip('/') in ('/ping', ''):
            # ANSWERED WITHOUT TOUCHING adsk.*, so a liveness check still works
            # while the main thread is busy or blocked. A ping that queued behind
            # a modal dialog would report "dead" for a perfectly healthy Fusion.
            self._send(200, {'ok': True, 'bridge': 'DwmBridge', 'version': 1})
        else:
            self._send(404, {'ok': False, 'error': 'Unknown path: %s' % self.path})

    def do_POST(self):
        command = self.path.strip('/')
        fn = COMMANDS.get(command)
        if fn is None:
            self._send(404, {'ok': False,
                             'error': 'Unknown command "%s". Known: %s'
                                      % (command, ', '.join(sorted(COMMANDS)))})
            return

        try:
            length = int(self.headers.get('Content-Length') or 0)
            raw = self.rfile.read(length) if length else b'{}'
            payload = json.loads(raw.decode('utf-8') or '{}')
        except Exception as exc:
            self._send(400, {'ok': False, 'error': 'Bad JSON body: %s' % exc})
            return

        try:
            result = _on_main(lambda: fn(payload))
            result = dict(result or {})
            result['ok'] = True
            self._send(200, result)
        except Exception as exc:
            # HTTP 200 WITH ok:false. The failure is Fusion's, not the transport's,
            # and the client checks both layers -- a 500 here would suggest the
            # bridge itself broke.
            self._send(200, {'ok': False, 'error': str(exc)})


# =============================================================================
# ADD-IN LIFECYCLE
# =============================================================================

def run(context):
    global _app, _ui, _server, _server_thread, _event_handler, _custom_event
    try:
        _app = adsk.core.Application.get()
        _ui = _app.userInterface

        # Unregister first: a reload without this leaves the old registration and
        # fireCustomEvent silently does nothing.
        try:
            _app.unregisterCustomEvent(CUSTOM_EVENT_ID)
        except Exception:
            pass

        _custom_event = _app.registerCustomEvent(CUSTOM_EVENT_ID)
        _event_handler = _JobHandler()
        _custom_event.add(_event_handler)
        _handlers.append(_event_handler)          # keep it alive; see header

        _server = ThreadingHTTPServer((HOST, PORT), _Handler)
        _server.daemon_threads = True
        _server_thread = threading.Thread(target=_server.serve_forever, daemon=True)
        _server_thread.start()

        _app.log('DwmBridge listening on http://%s:%d/' % (HOST, PORT))

    except Exception:
        if _ui:
            _ui.messageBox('DwmBridge failed to start:\n\n%s' % traceback.format_exc(),
                           'DwmBridge')


def stop(context):
    global _server, _server_thread, _custom_event
    try:
        if _server is not None:
            # BOTH CALLS MATTER. shutdown() stops the loop; server_close() releases
            # the socket. Without the second, reloading the add-in fails to bind
            # 18750 and looks like "port already in use" by a Fusion that is gone.
            _server.shutdown()
            _server.server_close()
            _server = None
        _server_thread = None

        if _custom_event is not None:
            for h in _handlers:
                try:
                    _custom_event.remove(h)
                except Exception:
                    pass
            _custom_event = None
        del _handlers[:]

        try:
            _app.unregisterCustomEvent(CUSTOM_EVENT_ID)
        except Exception:
            pass

    except Exception:
        if _ui:
            _ui.messageBox('DwmBridge failed to stop cleanly:\n\n%s'
                           % traceback.format_exc(), 'DwmBridge')

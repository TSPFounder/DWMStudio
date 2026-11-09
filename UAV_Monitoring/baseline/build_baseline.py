"""Build the UAV monitoring OOSEM baseline and its derived review documents.

Single source of truth for the project content. Running this file rewrites:

  baseline/uav_baseline.json          everything below, validated
  baseline/wbs_elements.tsv           WBS rows the Simulink builders read
  baseline/requirement_allocation.tsv requirement -> WBS element -> block
  documents/UAV_Requirements_Specification.docx   one bookmark per requirement
  documents/UAV_Verification_Matrix.xlsx          needs, requirements, trace, ledger
  documents/trace_manifest.csv        cross-tool trace rows (plan section 7)
  documents/task_ledger.csv           work packets (plan section 5)

Numeric acceptance thresholds are deliberately absent. The plan leaves them
TBD until gate G2, so every such value is the literal text "TBD (G2)".

Standard library plus python-docx and openpyxl.  python build_baseline.py
"""
import csv
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
DOCS = ROOT / "documents"
BASELINE_ID = "B01"
TBD = "TBD (G2)"

STAKEHOLDERS = [
    dict(id="DAVE", name="Dave", role="Father and husband",
         banner="My stories involve monitoring my family at home and while traveling and sending a response in an emergency.",
         source="Storyboard image supplied in this session"),
    dict(id="SALLY", name="Sally", role="Farmer, horse owner and farm owner",
         banner="My stories involve monitoring my property for intrusion by poachers, predators, etc. and sending a response.",
         source="Storyboard image supplied in this session"),
    dict(id="KIM", name="Kim", role="Student",
         banner="My stories involve monitoring me when walking around campus for protection and response to an assault.",
         source="Storyboard image supplied in this session"),
    dict(id="JEN", name="Jen", role="Homeowner",
         banner="My stories involve monitoring my business when I'm away to notify emergency responders if there is a fire, break-in, or other emergency.",
         source="Storyboard image supplied in this session"),
    dict(id="BILL", name="Bill", role="Son and caregiver of an elderly mother",
         banner="",
         source="Plan section 2 summary only; storyboard image not supplied in this session"),
    dict(id="TOM", name="Tom", role="Farmer",
         banner="",
         source="Plan section 2 summary only; storyboard image not supplied in this session"),
]

EXCLUDED_SCENES = [
    dict(stakeholder="DAVE", scene="Scene 4", reason="Placeholder title and empty description"),
    dict(stakeholder="DAVE", scene="Scene 5", reason="Placeholder title and empty description"),
    dict(stakeholder="DAVE", scene="Scene 6", reason="Placeholder title and empty description"),
    dict(stakeholder="KIM", scene="Scene 3", reason="No narrative; need text is copied from Dave's storyboard"),
    dict(stakeholder="JEN", scene="Scene 3", reason="Empty scene and empty need"),
]

OPEN_QUESTIONS = [
    "Jen's banner says business; her two completed scenes describe a home. Which is intended?",
    "Dave scene 1 is titled Entering & Exiting My House but its need text refers to the car. Confirm the setting.",
    "Sally scene 1 need reads 'to track any And: raise a loud alarm'. The tracked object is missing; 'predator' is assumed.",
    "Kim scenes 1 and 2 keep the placeholder titles Scene 1 Title and Scene 2 Title.",
    "Bill and Tom storyboard wording was not available here; their needs use the plan's summaries until the source is reviewed.",
    "Pilot property, stakeholder representative, operating conditions, budget, staffing and response recipients (plan section 11).",
    "Acceptable missed-event and nuisance-alert rates, and every threshold marked TBD (G2).",
]

M_FARM, M_HOME, M_PERSON = "FARM", "HOME", "PERSON"
MISSIONS = [
    dict(key=M_FARM, name="Farm Monitoring Demonstrator",
         theme="Outdoor property and livestock observation",
         description="Priority mission. Observe a defined farm property, find cattle outside their enclosure and "
                     "other events needing the owner's attention, and deliver an actionable notification.",
         narrative="Start of the limited Family of Systems: an outdoor UAV, fixed sensing and a shared operator "
                   "and notification service. Response decisions stay with people and external services.",
         stakeholders=["TOM", "SALLY", "JEN"]),
    dict(key=M_HOME, name="Home and Family Awareness",
         theme="Fixed sensing first, optional exterior UAV observation",
         description="Awareness and an evidence record around the home and car. Fixed and personal sensing first; "
                     "a UAV only in an approved operating setting.",
         narrative="Extension increment H. Reuses the notification service; no automatic inheritance of aircraft suitability.",
         stakeholders=["DAVE", "JEN"]),
    dict(key=M_PERSON, name="Personal Safety and Assistance",
         theme="Wearable and campus systems first",
         description="User-initiated or confirmed assistance requests with location and context, without medical diagnosis.",
         narrative="Extension increment H. Tested first with the non-airborne members of the Family of Systems.",
         stakeholders=["KIM", "BILL"]),
]


def need(id_, stakeholder, mission, scene, as_a, i_need, so_that, normalized, allocation, verbatim=True):
    return dict(id=id_, stakeholder=stakeholder, mission=mission, scene=scene, as_a=as_a, i_need=i_need,
                so_that=so_that, normalized=normalized, allocation=allocation,
                source_wording="verbatim from storyboard" if verbatim else "plan section 2 summary; storyboard not reviewed here",
                owner=stakeholder, validation="Stakeholder review at G1; outcome check at G7")


NEEDS = [
    need("D01", "DAVE", M_HOME, "Entering & Exiting My House", "father & husband",
         "a mobile security system to monitor my family when entering and exiting my car And: to track anyone that "
         "approaches us And: record the area around my car with video and audio",
         "there is a visual and audible record of all interactions when entering and exiting my car.",
         "Family awareness and a record of encounters when arriving at or leaving the home or car.",
         "Fixed and mobile sensing, optional UAV coverage"),
    need("D02", "DAVE", M_HOME, "Entering & Exiting My Car", "father & husband",
         "a mobile security system to monitor my family when traveling around town And: to track anyone that "
         "approaches them And: send an alert to emergency responders",
         "my children are protected And: there is a visual and audible record of all interactions when entering and exiting my car.",
         "Capture an incident near the car in town and request assistance.",
         "Personal device first; UAV only in an approved operating setting"),
    need("D03", "DAVE", M_HOME, "Inside My Home", "father & husband",
         "a mobile security system to monitor my family inside our house",
         "an silent alarm is triggerd and there is a visual and audible record of all interactions when police apprehend an intruder.",
         "Silent notification and evidence when an intruder is inside the home.",
         "Fixed home sensing first"),
    need("S01", "SALLY", M_FARM, "Predators", "farmer with livestock",
         "a mobile security system to monitor my livestock And: to track any And: raise a loud alarm",
         "the predator is scared away",
         "Detect a concerning animal encounter near livestock and alert the owner, with a controlled alarm option.",
         "UAV or fixed observation; alarm only when authorized"),
    need("S02", "SALLY", M_FARM, "Horse Thieves", "horse owner",
         "a mobile security system to monitor my horses when they are turned out And: to track anyone that "
         "approaches them And: record the thief And: to notify the police",
         "the police can apprehend the thief",
         "Observe an incident near turned-out horses, preserve evidence and notify the owner or response service.",
         "UAV or fixed observation; notification service"),
    need("S03", "SALLY", M_FARM, "Poachers", "farm owner",
         "a mobile security system to monitor my entire farm And: to track anyone that approaches And: record them "
         "And: to notify the police",
         "the police can apprehend the poacher",
         "Observe activity on the property and route evidence for human assessment.",
         "UAV observation; notification service"),
    need("K01", "KIM", M_PERSON, "Scene 1 Title (walking home from a late test)", "student",
         "a mobile security system to monitor me when I walk across campus And: to track anyone that approaches me "
         "And: record the area around me And: raise an audible alarm",
         "the mugger is scared away",
         "User-initiated or confirmed assistance, evidence and an audible alarm at night on campus.",
         "Personal and campus systems first"),
    need("K02", "KIM", M_PERSON, "Scene 2 Title (walking home from a class)", "student",
         "a mobile security system to monitor me when entering I walk across campus And: to track anyone that "
         "verbally abuses me And: records the comments",
         "the university has evidence of the event",
         "Record and report an encounter with appropriate consent and access controls.",
         "Personal device; evidence store"),
    need("J01", "JEN", M_HOME, "House Fire", "a homeowner",
         "a mobile security system to monitor my home when I'm gone And: to notify the fire department if my house catches on fire",
         "the fire can be put out quickly",
         "Receive a fire alarm while away and share situational information.",
         "Existing fixed detection first; optional exterior UAV observation"),
    need("J02", "JEN", M_FARM, "Fires on Property", "a homeowner",
         "a mobile security system to monitor my property when I'm gone And: to notify the fire department if my property catches on fire",
         "the fire can be put out quickly",
         "Observe a fire on the property and notify; exterior information when operation is authorized.",
         "UAV exterior observation; notification service"),
    need("B01", "BILL", M_PERSON, "Mother unable to leave bed", "son and caregiver",
         "", "", "Obtain an assistance request and notify a caregiver, without medical diagnosis.",
         "Wearable or fixed device", verbatim=False),
    need("B02", "BILL", M_PERSON, "Mother locked out", "son and caregiver",
         "", "", "Request assistance and give a responder useful location and context.",
         "Wearable device; notification service", verbatim=False),
    need("B03", "BILL", M_PERSON, "Mother falls from ladder", "son and caregiver",
         "", "", "Detect or report a possible fall and request help; confirm with the person or caregiver when possible.",
         "Wearable or fixed device", verbatim=False),
    need("T01", "TOM", M_FARM, "Cattle Monitoring", "farmer",
         "", "", "Farmer needs actionable awareness of cattle outside the defined enclosure so recovery can begin.",
         "UAV or fixed sensor observation; shared notification service", verbatim=False),
    need("T02", "TOM", M_FARM, "Crop distress", "farmer",
         "", "", "Identify crop areas needing inspection; validate observations against agronomic ground truth.",
         "UAV observation", verbatim=False),
    need("T03", "TOM", M_FARM, "Orchard ripeness", "farmer",
         "", "", "Identify fruit ready for inspection or harvest; validate by crop and growing conditions.",
         "UAV observation", verbatim=False),
]

USE_CASES = [
    dict(id="UC-T01", mission=M_FARM, need="T01", actor="TOM", name="Monitor enclosure and report escaped cattle",
         description="Monitor enclosure, detect candidate escape, confirm location, notify, obtain acknowledgment and close event.",
         steps=["Operator approves the patrol and the enclosure boundary for the operating envelope",
                "UAV or fixed sensor observes the enclosure boundary",
                "System detects a candidate boundary crossing and estimates its location",
                "System compares the location with the enclosure and confirms or rejects the candidate",
                "Notification service sends the event with time, location, uncertainty and image evidence",
                "Farmer acknowledges; unacknowledged events escalate to the named alternate",
                "On loss of observation or delivery, the system reports unavailable coverage",
                "Farmer closes the event after recovery"]),
    dict(id="UC-S01", mission=M_FARM, need="S01", actor="SALLY", name="Report animal encounter near livestock",
         description="Observe livestock, report a concerning animal encounter and sound an alarm only when the owner authorizes it.",
         steps=["System observes the livestock area", "System reports a candidate encounter with evidence and uncertainty",
                "Owner reviews and may authorize the audible alarm", "System records the outcome"]),
    dict(id="UC-S02", mission=M_FARM, need="S02", actor="SALLY", name="Observe property incident and preserve evidence",
         description="Covers S02, S03 and J02: observe a person, vehicle or fire on the property, preserve evidence and notify.",
         steps=["System observes the defined area", "System reports the observation without classifying intent",
                "Evidence is stored with access control", "Owner decides whether to contact an external response service"]),
    dict(id="UC-T02", mission=M_FARM, need="T02", actor="TOM", name="Survey crop and orchard condition",
         description="Covers T02 and T03: collect imagery over fields or orchard blocks and flag areas for inspection.",
         steps=["Operator selects the survey area", "UAV collects imagery", "System flags candidate areas",
                "Farmer inspects and records ground truth"]),
    dict(id="UC-D01", mission=M_HOME, need="D01", actor="DAVE", name="Record arrivals, departures and intrusions",
         description="Covers D01 to D03 and J01 with fixed and personal sensing; UAV coverage is optional.",
         steps=["Fixed or personal sensor detects an event", "System records video and audio under the agreed consent rules",
                "System notifies the family silently when configured", "Family member decides on external response"]),
    dict(id="UC-K01", mission=M_PERSON, need="K01", actor="KIM", name="Request assistance and record an encounter",
         description="Covers K01, K02 and B01 to B03: a person requests or confirms help; the system shares location and context.",
         steps=["Person requests help or the device detects a possible fall", "System asks the person to confirm when possible",
                "System notifies the caregiver or campus service with location and context",
                "Recipient acknowledges; unacknowledged requests escalate"]),
]

MISSION_REQUIREMENTS = [
    dict(number="MR-T01", mission=M_FARM, needs=["T01"], title="Actionable awareness of escaped cattle",
         text="The Family of Systems shall give the farmer timely, actionable awareness of cattle outside the defined "
              "enclosure within the approved operating envelope. Effectiveness measure MOE-T01; threshold " + TBD + "."),
    dict(number="MR-S01", mission=M_FARM, needs=["S01"], title="Alert on animal encounter near livestock",
         text="The Family of Systems shall alert the owner to a concerning animal encounter near livestock and shall "
              "sound an audible alarm only on the owner's authorization."),
    dict(number="MR-S02", mission=M_FARM, needs=["S02", "S03", "J02"], title="Observe property incidents and preserve evidence",
         text="The Family of Systems shall observe incidents on the defined property, preserve evidence and notify the "
              "owner, leaving assessment and external response to people."),
    dict(number="MR-T02", mission=M_FARM, needs=["T02", "T03"], title="Flag crop and orchard areas for inspection",
         text="The Family of Systems shall identify crop and orchard areas for inspection, validated against ground truth "
              "by crop and season."),
    dict(number="MR-D01", mission=M_HOME, needs=["D01", "D02", "D03", "J01"], title="Home and family awareness with evidence",
         text="The Family of Systems shall provide awareness and an access-controlled record of encounters and alarms at "
              "the home and car using fixed and personal sensing first."),
    dict(number="MR-K01", mission=M_PERSON, needs=["K01", "K02", "B01", "B02", "B03"], title="Assistance request with location and context",
         text="The Family of Systems shall deliver a user-initiated or confirmed assistance request with location and "
              "context to a named recipient, without medical diagnosis."),
]

UAV, FIX, OPS = "Outdoor UAV", "Fixed and Wearable Sensing", "Operator and Notification Service"


def sr(number, title, text, type_, verification, derived, systems, wbs, mission=M_FARM):
    return dict(number=number, title=title, text=text, type=type_, verification=verification, priority="Shall",
                mission=mission, derived_from=derived, allocated_to=systems, wbs=wbs,
                threshold_status="TBD at G2" if "TBD" in text else "No numeric threshold")


SYSTEM_REQUIREMENTS = [
    sr("SR-T01-01", "Report confirmed boundary crossing",
       "Within the approved operating envelope, the system shall report a confirmed enclosure boundary crossing within "
       "latency L after the ground-truth crossing time. L is " + TBD + ".",
       "Performance", "Test", ["MR-T01"], [UAV, OPS], ["1.1.6", "1.3.2", "1.3.3"]),
    sr("SR-T01-02", "Crossing report content",
       "Each crossing report shall contain event time, location with uncertainty, image evidence and delivery state. "
       "Acceptance criteria are " + TBD + ".",
       "Functional", "Inspection", ["MR-T01"], [UAV, OPS], ["1.2.1", "1.3.2", "1.3.4"]),
    sr("SR-T01-03", "Indicate unavailable coverage",
       "On loss of observation or delivery capability, the system shall indicate unavailable coverage and follow the "
       "approved degraded mode.",
       "Functional", "Demonstration", ["MR-T01"], [UAV, OPS], ["1.1.5", "1.3.2", "1.3.3"]),
    sr("SR-UAV-01", "Patrol coverage of the enclosure",
       "The UAV shall observe the defined enclosure boundary with a revisit interval of R. R is " + TBD + ".",
       "Performance", "Analysis", ["MR-T01", "MR-S02"], [UAV], ["1.1.1", "1.1.4", "1.2.1"]),
    sr("SR-UAV-02", "Patrol energy with reserve",
       "The UAV shall complete the approved patrol and return with a usable-energy reserve of E_res. E_res is " + TBD + ".",
       "Performance", "Analysis", ["MR-T01"], [UAV], ["1.1.2", "1.1.3"]),
    sr("SR-UAV-03", "Geolocate an observation",
       "The UAV shall estimate the ground location of an observation and report its uncertainty. The error limit is " + TBD + ".",
       "Performance", "Test", ["MR-T01", "MR-S02"], [UAV], ["1.1.4", "1.1.6", "1.2.1"]),
    sr("SR-UAV-04", "Link status and loss-of-link behavior",
       "The UAV shall report command and telemetry link status and, on loss of link, shall follow the approved failsafe.",
       "Functional", "Test", ["MR-T01"], [UAV], ["1.1.4", "1.1.5"]),
    sr("SR-UAV-05", "Time-stamped image evidence",
       "The payload shall capture image evidence time-stamped against the common time source.",
       "Functional", "Test", ["MR-T01", "MR-S02", "MR-T02"], [UAV], ["1.2.1"]),
    sr("SR-UAV-06", "Authorized audible alarm only",
       "The UAV shall sound its audible alarm only while an operator authorization is active.",
       "Constraint", "Demonstration", ["MR-S01"], [UAV, OPS], ["1.2.2", "1.3.1"]),
    sr("SR-NOT-01", "Delivery state, acknowledgment and escalation",
       "The notification service shall record delivery state and recipient acknowledgment for each event and shall "
       "escalate an unacknowledged event to the named alternate after T_ack. T_ack is " + TBD + ".",
       "Functional", "Test", ["MR-T01", "MR-S01", "MR-S02", "MR-D01", "MR-K01"], [OPS], ["1.3.3"]),
    sr("SR-NOT-02", "Evidence retention and access control",
       "The evidence store shall keep recordings under the access and retention rules approved for the deployment.",
       "Constraint", "Inspection", ["MR-S02", "MR-D01", "MR-K01"], [OPS], ["1.3.4"]),
    sr("SR-FIX-01", "Fixed and wearable events without an aircraft",
       "Fixed and wearable devices shall send their events to the notification service without an aircraft being airborne.",
       "Interface", "Test", ["MR-D01", "MR-K01", "MR-T01"], [FIX, OPS], ["1.4.1", "1.4.2", "1.3.2"]),
    sr("SR-GEN-01", "Report observations, not judgments",
       "The system shall report observations with their uncertainty and shall not label a person as a criminal.",
       "Constraint", "Inspection", ["MR-S02", "MR-D01", "MR-K01"], [UAV, FIX, OPS], ["1.1.6", "1.3.2"]),
    sr("SR-GEN-02", "Common time source",
       "All event, evidence and delivery timestamps shall be traceable to one common time source.",
       "Interface", "Test", ["MR-T01"], [UAV, FIX, OPS], ["1.1.6", "1.3.2", "1.4.1"]),
]

# System hierarchy: Family of Systems -> SoI -> subsystem. Each leaf names its WBS code.
SYSTEMS = dict(name="Monitoring and Assistance Family of Systems", type="FoS",
               description="Outdoor UAV, fixed or wearable sensing, and a shared operator and notification service. "
                           "Emergency responders are external actors.",
               children=[
                   dict(name=UAV, type="SoI", wbs="1.1", description="Mobile outdoor observation. No LLM in the flight control loop.",
                        children=[
                            dict(name="Airframe", type="Subsystem", wbs="1.1.1", description="Structure, lifting surfaces or arms, landing gear."),
                            dict(name="Propulsion", type="Subsystem", wbs="1.1.2", description="Motors, speed controllers and propellers."),
                            dict(name="Electrical Power", type="Subsystem", wbs="1.1.3", description="Battery, distribution and monitoring."),
                            dict(name="Flight Control", type="Subsystem", wbs="1.1.4", description="Guidance, navigation and control; failsafe."),
                            dict(name="Communications", type="Subsystem", wbs="1.1.5", description="Command, telemetry and payload links."),
                            dict(name="Onboard Processing", type="Subsystem", wbs="1.1.6", description="Detection, geolocation and enclosure comparison."),
                            dict(name="EO Camera", type="Subsystem", wbs="1.2.1", description="Visible-light camera payload."),
                            dict(name="Audible Alarm", type="Subsystem", wbs="1.2.2", description="Alarm sounded only on authorization."),
                        ]),
                   dict(name=OPS, type="SoI", wbs="1.3", description="Shared ground service for operators and recipients.",
                        children=[
                            dict(name="Ground Control Station", type="Subsystem", wbs="1.3.1", description="Mission approval, monitoring, alarm authorization."),
                            dict(name="Event Manager", type="Subsystem", wbs="1.3.2", description="Confirms candidates, manages event state."),
                            dict(name="Notification Gateway", type="Subsystem", wbs="1.3.3", description="Delivery, acknowledgment, escalation."),
                            dict(name="Evidence Store", type="Subsystem", wbs="1.3.4", description="Access-controlled recordings and logs."),
                        ]),
                   dict(name=FIX, type="SoI", wbs="1.4", description="Indoor continuity and personal assistance without an aircraft.",
                        children=[
                            dict(name="Fixed Camera Node", type="Subsystem", wbs="1.4.1", description="Fixed observation of a boundary or entrance."),
                            dict(name="Wearable Assist Device", type="Subsystem", wbs="1.4.2", description="Assistance request and possible-fall report."),
                        ]),
               ])

# WBS. simulink=True marks the elements that become Subsystem blocks.
WBS = dict(name="UAV Monitoring System WBS", version="1", control_account_level=3, mapped_system=UAV, elements=[
    dict(code="1.0", name="UAV Monitoring System", type="SoS", simulink=False, description="Limited Family of Systems, farm demonstrator first."),
    dict(code="1.1", name="Air Vehicle", type="SoI", simulink=True, description="Outdoor UAV air vehicle."),
    dict(code="1.1.1", name="Airframe", type="Subsystem", simulink=True, description="Point-mass kinematics in the mission model."),
    dict(code="1.1.2", name="Propulsion", type="Subsystem", simulink=True, description="Electrical power drawn against airspeed."),
    dict(code="1.1.3", name="Electrical Power", type="Subsystem", simulink=True, description="Usable energy, state of charge, reserve flag."),
    dict(code="1.1.4", name="Flight Control", type="Subsystem", simulink=True, description="Patrol guidance: speed and turn-rate commands."),
    dict(code="1.1.5", name="Communications", type="Subsystem", simulink=True, description="Link availability against range."),
    dict(code="1.1.6", name="Onboard Processing", type="Subsystem", simulink=True, description="Enclosure comparison and candidate detection."),
    dict(code="1.2", name="Payload", type="SoI", simulink=True, description="Mission payloads."),
    dict(code="1.2.1", name="EO Camera", type="Subsystem", simulink=True, description="Target inside the ground footprint."),
    dict(code="1.2.2", name="Audible Alarm", type="Subsystem", simulink=True, description="Alarm gated by authorization."),
    dict(code="1.3", name="Ground Segment", type="SoI", simulink=True, description="Operator and notification service."),
    dict(code="1.3.1", name="Ground Control Station", type="Subsystem", simulink=True, description="Alarm authorization pass-through."),
    dict(code="1.3.2", name="Event Manager", type="Subsystem", simulink=True, description="Confirms a candidate when a link is available."),
    dict(code="1.3.3", name="Notification Gateway", type="Subsystem", simulink=True, description="Delivery delay to the recipient."),
    dict(code="1.3.4", name="Evidence Store", type="Subsystem", simulink=True, description="Accumulated evidence time."),
    dict(code="1.4", name="Fixed and Wearable Sensing", type="SoI", simulink=True, description="Non-airborne members."),
    dict(code="1.4.1", name="Fixed Camera Node", type="Subsystem", simulink=True, description="Target inside a fixed node's radius."),
    dict(code="1.4.2", name="Wearable Assist Device", type="Subsystem", simulink=True, description="Assistance request pass-through."),
    dict(code="1.5", name="Systems Engineering and Program Management", type="Other", simulink=False, description="Baselines, reviews, ledger."),
    dict(code="1.6", name="System Test and Evaluation", type="Other", simulink=False, description="Simulation and field test."),
    dict(code="1.6.1", name="Simulation Environment", type="Other", simulink=False, description="Simulink, Simscape and Unreal scenarios."),
    dict(code="1.6.2", name="Field Test", type="Other", simulink=False, description="Staged crossings with independent ground truth."),
])

MEASURES = [
    dict(id="MOE-T01", kind="MoE", need="T01", name="Actionable escape awareness",
         definition="Fraction of staged escapes that lead to a timely, actionable farmer acknowledgment.",
         denominator="Staged escapes inside the approved operating envelope", threshold=TBD),
    dict(id="MOP-T01-PD", kind="MoP", need="T01", name="Detection probability",
         definition="Staged crossings detected / staged crossings observed under stated conditions.", threshold=TBD),
    dict(id="MOP-T01-FA", kind="MoP", need="T01", name="False alerts per monitored hour",
         definition="Alerts with no ground-truth crossing / monitored hours.", threshold=TBD),
    dict(id="MOP-T01-LOC", kind="MoP", need="T01", name="Localization error",
         definition="Horizontal distance between reported and surveyed crossing location.", threshold=TBD),
    dict(id="MOP-T01-LAT", kind="MoP", need="T01", name="Notification latency",
         definition="Recipient delivery time minus ground-truth crossing time.", threshold=TBD),
    dict(id="MOP-T01-AV", kind="MoP", need="T01", name="Mission availability",
         definition="Time with coverage available / scheduled monitoring time.", threshold=TBD),
]

ANALYSES = [
    dict(id="ANA-T01-MISSION", type="Simulink", name="T01 mission behavior (R2011a WBS model)",
         description="Patrol, detection, link and notification timing in uav_t01_mission.mdl. Simulation evidence only.",
         requirements=["SR-T01-01", "SR-T01-03", "SR-UAV-01", "SR-UAV-04"]),
    dict(id="ANA-T01-ENERGY", type="Simulink", name="T01 patrol energy",
         description="Energy drawn over the patrol against usable energy and reserve. Parameters are provisional.",
         requirements=["SR-UAV-02"]),
    dict(id="ANA-PWR-SIMSCAPE", type="Simulink", name="Propulsion and power physical model (Simscape, R2025b)",
         description="Battery, motor and propeller load network in uav_power_simscape.slx.",
         requirements=["SR-UAV-02"]),
    dict(id="ANA-AERO-DATCOM", type="Datcom", name="VTOL fixed-wing candidate aerodynamics",
         description="Digital DATCOM run of the fixed-wing candidate for the airframe trade. Applicability limits recorded.",
         requirements=["SR-UAV-01", "SR-UAV-02"]),
]

SIMULATION = dict(name="T01 escaped cattle scenario (Unreal)",
                  description="Simulink co-simulation scene for the T01 scenario. Synthetic scene; not detection evidence.")

DESIGNS = [
    dict(name="UAV - Assembly", type="CAD_Assembly", system=UAV, wbs="1.1"),
    dict(name="UAV Airframe", type="CAD_Part", system="Airframe", wbs="1.1.1"),
    dict(name="UAV Motor Arm", type="CAD_Part", system="Airframe", wbs="1.1.1"),
    dict(name="UAV Motor Mount", type="CAD_Part", system="Propulsion", wbs="1.1.2"),
    dict(name="UAV Payload Tray", type="CAD_Part", system="EO Camera", wbs="1.2.1"),
    dict(name="UAV Airframe Drawing", type="CAD_Drawing", system="Airframe", wbs="1.1.1"),
]

VERIFICATION = [
    dict(id="TEST-T01-01", requirement="SR-T01-01", method="Test", level="System",
         procedure="Staged crossings and non-crossing controls with independent, time-stamped ground truth; compare "
                   "event and delivery logs.", measured="Notification latency", threshold=TBD, result="Not run"),
    dict(id="TEST-T01-02", requirement="SR-T01-02", method="Inspection", level="System",
         procedure="Inspect each delivered report for the required fields.", measured="Field presence", threshold=TBD, result="Not run"),
    dict(id="TEST-T01-03", requirement="SR-T01-03", method="Demonstration", level="System",
         procedure="Remove observation, then delivery; confirm the unavailable-coverage indication and degraded mode.",
         measured="Indication shown", threshold="Indicated", result="Not run"),
    dict(id="ANA-T01-MISSION", requirement="SR-UAV-01", method="Analysis", level="System",
         procedure="Run uav_t01_mission; compute the revisit interval from the patrol.", measured="Revisit interval", threshold=TBD, result="Not run"),
    dict(id="ANA-T01-ENERGY", requirement="SR-UAV-02", method="Analysis", level="Subsystem",
         procedure="Run the energy analysis with the reviewed mission segments; independent check of units and reserve.",
         measured="Energy remaining at landing", threshold=TBD, result="Not run"),
]

LEDGER = [
    dict(task="TASK-A-001", role="Configuration and tool steward", increment="A",
         objective="Inventory PD Quick, MATLAB R2011a and R2025b, UModel, Fusion and Unreal; record versions and licenses.",
         inputs=BASELINE_ID, outputs="Tool readiness record", reviewer="Project engineer", state="Ready"),
    dict(task="TASK-A-002", role="Configuration and tool steward", increment="A",
         objective="Round trip: one PD Quick record, one native diagram and one deterministic analysis saved and read back.",
         inputs=BASELINE_ID, outputs="uav_t01_selftest result; seeded database readback", reviewer="V&V", state="Ready"),
    dict(task="TASK-B-001", role="Needs and operations analyst", increment="B",
         objective="Seed the source register: 6 stakeholders, 16 needs, 5 excluded scenes, open questions.",
         inputs=BASELINE_ID, outputs="PD Quick Missions records", reviewer="Human sponsor", state="Ready"),
    dict(task="TASK-B-002", role="Needs and operations analyst", increment="B",
         objective="Review each need with its stakeholder; resolve the open questions; record G1 disposition.",
         inputs="TASK-B-001", outputs="G1 package", reviewer="Human sponsor", state="Blocked: needs stakeholder review"),
    dict(task="TASK-C-001", role="Requirements and model engineer", increment="C",
         objective="Derive mission and system requirements; allocate to systems and WBS; export catalogs.",
         inputs="TASK-B-001", outputs="PD Quick Requirements; .pdqreq and .xlsx", reviewer="Chief systems engineer", state="Ready"),
    dict(task="TASK-C-002", role="Requirements and model engineer", increment="C",
         objective="Create UModel requirement, block definition and use case diagrams with deriveReqt, satisfy and verify.",
         inputs="TASK-C-001", outputs="SystemModel.ump diagrams", reviewer="Chief systems engineer", state="Ready"),
    dict(task="TASK-C-003", role="Requirements and model engineer", increment="C",
         objective="Build the R2011a WBS model; link blocks to PD Quick, Excel and Word requirements with the V&V toolbox.",
         inputs="TASK-C-001", outputs="uav_wbs_lib.mdl, uav_t01_mission.mdl, links report", reviewer="V&V", state="Ready"),
    dict(task="TASK-D-001", role="Analysis agent (aerodynamics)", increment="D",
         objective="Run the DATCOM fixed-wing candidate; record applicability limits.",
         inputs="TASK-C-001", outputs="datcom output and coefficients table", reviewer="Chief systems engineer", state="Ready"),
    dict(task="TASK-D-002", role="Design engineer (airframe)", increment="D",
         objective="Create the multirotor candidate parts, assembly and drawing in Fusion; record mass properties.",
         inputs="TASK-C-001", outputs="Fusion documents; mass properties JSON", reviewer="Chief systems engineer", state="Ready"),
    dict(task="TASK-D-003", role="Analysis agent (power)", increment="D",
         objective="Build and check the Simscape propulsion and power model in R2025b.",
         inputs="TASK-C-003", outputs="uav_power_simscape.slx; steady-state check", reviewer="V&V", state="Ready"),
    dict(task="TASK-E-001", role="Software and integration engineer", increment="E",
         objective="Pull the R2011a subsystems into an R2025b model; compare both releases back to back.",
         inputs="TASK-C-003", outputs="uav_t01_mission_r2025b.slx; comparison report", reviewer="V&V", state="Ready"),
    dict(task="TASK-E-002", role="Software and integration engineer", increment="E",
         objective="Run the T01 scenario with the Unreal scene in the loop.",
         inputs="TASK-E-001", outputs="uav_t01_ue.slx; run log", reviewer="V&V", state="Ready"),
    dict(task="TASK-F-001", role="Human technical authority", increment="F",
         objective="Approve release to fabrication, powered tests and flight.",
         inputs="G4, G5 packages", outputs="Signed approval", reviewer="Human test authority", state="Blocked: human decision"),
]

# Mission model parameters. Provisional engineering values for a deterministic
# self-test, NOT approved requirements and NOT a sized aircraft.
MODEL_PARAMETERS = dict(
    status="Provisional values for a deterministic self-test; not approved thresholds or a design",
    patrol_radius_m=150.0, patrol_speed_mps=12.0, enclosure_half_width_m=100.0,
    camera_footprint_radius_m=60.0, link_range_m=1000.0, notify_delay_s=5.0,
    cattle_start_east_m=80.0, cattle_speed_mps=1.0, cattle_depart_time_s=20.0,
    hover_power_w=180.0, drag_power_coeff_w_per_mps3=0.05, usable_energy_wh=90.0, reserve_fraction=0.2,
    fixed_node_east_m=130.0, fixed_node_radius_m=25.0, stop_time_s=300.0,
    fixed_step_s=0.05)


def validate():
    errors = []
    need_ids = {n["id"] for n in NEEDS}
    mr = {m["number"] for m in MISSION_REQUIREMENTS}
    wbs = {e["code"] for e in WBS["elements"]}
    stake = {s["id"] for s in STAKEHOLDERS}
    missions = {m["key"] for m in MISSIONS}

    def names(node):
        yield node["name"]
        for child in node.get("children", []):
            yield from names(child)
    systems = set(names(SYSTEMS))
    if len(NEEDS) != 16:
        errors.append(f"expected 16 needs, found {len(NEEDS)}")
    covered = {n for m in MISSION_REQUIREMENTS for n in m["needs"]}
    errors += [f"orphan need {n}" for n in sorted(need_ids - covered)]
    errors += [f"mission requirement names unknown need {n}" for n in sorted(covered - need_ids)]
    for n in NEEDS:
        if n["stakeholder"] not in stake or n["mission"] not in missions:
            errors.append(f"need {n['id']} has unknown stakeholder or mission")
    derived = set()
    numbers = [r["number"] for r in SYSTEM_REQUIREMENTS]
    if len(numbers) != len(set(numbers)):
        errors.append("duplicate system requirement number")
    for r in SYSTEM_REQUIREMENTS:
        derived |= set(r["derived_from"])
        errors += [f"{r['number']} derives from unknown {m}" for m in r["derived_from"] if m not in mr]
        errors += [f"{r['number']} allocated to unknown system {s}" for s in r["allocated_to"] if s not in systems]
        errors += [f"{r['number']} names unknown WBS {c}" for c in r["wbs"] if c not in wbs]
        if not r["allocated_to"] or not r["wbs"] or not r["verification"]:
            errors.append(f"{r['number']} lacks allocation or verification method")
    errors += [f"mission requirement {m} has no derived system requirement" for m in sorted(mr - derived)]
    sr_numbers = set(numbers)
    for v in VERIFICATION:
        if v["requirement"] not in sr_numbers:
            errors.append(f"{v['id']} verifies unknown {v['requirement']}")
    for a in ANALYSES:
        errors += [f"{a['id']} names unknown {r}" for r in a["requirements"] if r not in sr_numbers]
    for e in WBS["elements"]:
        parent = e["code"].rsplit(".", 1)[0]
        if e["code"] != "1.0" and e["code"].count(".") == 2 and parent not in wbs:
            errors.append(f"WBS {e['code']} has no parent")

    def leaves(node):
        if "wbs" in node and node["wbs"] not in wbs:
            errors.append(f"system {node['name']} names unknown WBS {node['wbs']}")
        for child in node.get("children", []):
            leaves(child)
    leaves(SYSTEMS)
    for u in USE_CASES:
        if u["need"] not in need_ids or u["actor"] not in stake:
            errors.append(f"use case {u['id']} has unknown need or actor")
    if errors:
        raise SystemExit("Baseline is inconsistent:\n  " + "\n  ".join(errors))


def baseline():
    return dict(
        meta=dict(baseline=BASELINE_ID, project="UAV Monitoring and Assistance",
                  project_description="UAV-enabled monitoring and assistance system developed with OOSEM. Farm demonstrator first.",
                  method="OOSEM", plan="UAV-Agent-OOSEM-Plan.md, planning baseline 1, 8 November 2026",
                  pd_quick_commit="32306b655eba2cea6da5ff1f1b9cad215d5a40da",
                  threshold_policy="No numeric acceptance threshold is approved. Values marked TBD (G2) are set at gate G2."),
        stakeholders=STAKEHOLDERS, excluded_scenes=EXCLUDED_SCENES, open_questions=OPEN_QUESTIONS,
        missions=MISSIONS, needs=NEEDS, use_cases=USE_CASES, mission_requirements=MISSION_REQUIREMENTS,
        system_requirements=SYSTEM_REQUIREMENTS, systems=SYSTEMS, wbs=WBS, measures=MEASURES, analyses=ANALYSES,
        simulation=SIMULATION, designs=DESIGNS, verification=VERIFICATION, ledger=LEDGER,
        model_parameters=MODEL_PARAMETERS)


def block_path(code):
    """Simulink path of a WBS element inside the library, e.g. 1.1.1 -> 'WBS 1.1 Air Vehicle/WBS 1.1.1 Airframe'."""
    by_code = {e["code"]: e for e in WBS["elements"]}
    parts = code.split(".")
    chain = [".".join(parts[:k]) for k in range(2, len(parts) + 1)]
    return "/".join(f"WBS {c} {by_code[c]['name']}" for c in chain)


def write_tsv(path, header, rows):
    with open(path, "w", newline="\n", encoding="utf-8") as handle:
        handle.write("\t".join(header) + "\n")
        for row in rows:
            if any("\t" in str(v) or "\n" in str(v) for v in row):
                raise ValueError(f"tab or newline in TSV field: {row}")
            handle.write("\t".join(str(v) for v in row) + "\n")


def write_tables():
    elements = [e for e in WBS["elements"] if e["simulink"]]
    write_tsv(HERE / "wbs_elements.tsv", ["Code", "Level", "Name", "BlockPath", "Description"],
              [(e["code"], e["code"].count(".") + 1, e["name"], block_path(e["code"]), e["description"]) for e in elements])
    rows = []
    for r in SYSTEM_REQUIREMENTS:
        for code in r["wbs"]:
            rows.append((r["number"], code, block_path(code), r["title"]))
    write_tsv(HERE / "requirement_allocation.tsv", ["RequirementNumber", "WbsCode", "BlockPath", "Title"], rows)


def bookmark_name(number):
    return number.replace("-", "_")


def write_docx():
    from docx import Document
    from docx.oxml import OxmlElement
    from docx.oxml.ns import qn

    doc = Document()
    doc.add_heading("UAV Monitoring and Assistance: Requirements Specification", 0)
    doc.add_paragraph(f"Baseline {BASELINE_ID}. Generated from baseline/uav_baseline.json; edit the baseline, not this file. "
                      "Thresholds marked TBD (G2) are not approved values. Each requirement heading carries a Word "
                      "bookmark named after its number (SR-T01-01 is bookmark SR_T01_01) for Simulink requirement links.")
    counter = [0]

    def heading(text, number):
        paragraph = doc.add_heading(text, 2)
        counter[0] += 1
        start = OxmlElement("w:bookmarkStart")
        start.set(qn("w:id"), str(counter[0]))
        start.set(qn("w:name"), bookmark_name(number))
        end = OxmlElement("w:bookmarkEnd")
        end.set(qn("w:id"), str(counter[0]))
        paragraph._p.insert(0, start)
        paragraph._p.append(end)

    doc.add_heading("Mission requirements", 1)
    for m in MISSION_REQUIREMENTS:
        heading(f"{m['number']}  {m['title']}", m["number"])
        doc.add_paragraph(m["text"])
        doc.add_paragraph("Source needs: " + ", ".join(m["needs"]))
    doc.add_heading("System requirements", 1)
    for r in SYSTEM_REQUIREMENTS:
        heading(f"{r['number']}  {r['title']}", r["number"])
        doc.add_paragraph(r["text"])
        doc.add_paragraph(f"Type: {r['type']}.  Verification: {r['verification']}.  Derived from: {', '.join(r['derived_from'])}.")
        doc.add_paragraph(f"Allocated to: {', '.join(r['allocated_to'])}.  WBS: {', '.join(r['wbs'])}.")
    doc.save(DOCS / "UAV_Requirements_Specification.docx")


def write_xlsx():
    from openpyxl import Workbook
    from openpyxl.styles import Font

    book = Workbook()

    def sheet(title, header, rows, first=False):
        ws = book.active if first else book.create_sheet()
        ws.title = title
        ws.append(header)
        for cell in ws[1]:
            cell.font = Font(bold=True)
        for row in rows:
            ws.append(list(row))
        ws.freeze_panes = "A2"
        for index, name in enumerate(header):
            width = max([len(str(name))] + [len(str(r[index])) for r in rows]) if rows else len(name)
            ws.column_dimensions[ws.cell(1, index + 1).column_letter].width = min(max(width + 2, 10), 70)

    sheet("Requirements", ["Number", "Kind", "Title", "Text", "Type", "Verification", "Derived from", "Allocated to", "WBS"],
          [(m["number"], "Mission", m["title"], m["text"], "", "", ", ".join(m["needs"]), "", "") for m in MISSION_REQUIREMENTS] +
          [(r["number"], "System", r["title"], r["text"], r["type"], r["verification"], ", ".join(r["derived_from"]),
            ", ".join(r["allocated_to"]), ", ".join(r["wbs"])) for r in SYSTEM_REQUIREMENTS], first=True)
    sheet("Needs", ["ID", "Stakeholder", "Mission", "Scene", "As a", "I need", "So that", "Normalized need", "Initial allocation", "Source wording"],
          [(n["id"], n["stakeholder"], n["mission"], n["scene"], n["as_a"], n["i_need"], n["so_that"], n["normalized"],
            n["allocation"], n["source_wording"]) for n in NEEDS])
    sheet("Verification", ["ID", "Requirement", "Method", "Level", "Procedure", "Measured quantity", "Threshold", "Result"],
          [(v["id"], v["requirement"], v["method"], v["level"], v["procedure"], v["measured"], v["threshold"], v["result"]) for v in VERIFICATION])
    sheet("Measures", ["ID", "Kind", "Need", "Name", "Definition", "Threshold"],
          [(m["id"], m["kind"], m["need"], m["name"], m["definition"], m["threshold"]) for m in MEASURES])
    sheet("WBS", ["Code", "Name", "Type", "Simulink subsystem", "Description"],
          [(e["code"], e["name"], e["type"], "yes" if e["simulink"] else "no", e["description"]) for e in WBS["elements"]])
    sheet("Excluded scenes", ["Stakeholder", "Scene", "Reason"],
          [(e["stakeholder"], e["scene"], e["reason"]) for e in EXCLUDED_SCENES])
    sheet("Open questions", ["Question"], [(q,) for q in OPEN_QUESTIONS])
    book.save(DOCS / "UAV_Verification_Matrix.xlsx")


def write_csvs():
    with open(DOCS / "trace_manifest.csv", "w", newline="", encoding="utf-8") as handle:
        out = csv.writer(handle)
        out.writerow(["Baseline", "From", "Relation", "To", "ToKind"])
        for m in MISSION_REQUIREMENTS:
            for n in m["needs"]:
                out.writerow([BASELINE_ID, m["number"], "deriveReqt", "NEED " + n, "Need"])
        for r in SYSTEM_REQUIREMENTS:
            for m in r["derived_from"]:
                out.writerow([BASELINE_ID, r["number"], "deriveReqt", m, "MissionRequirement"])
            for s in r["allocated_to"]:
                out.writerow([BASELINE_ID, s, "satisfy", r["number"], "SystemRequirement"])
            for c in r["wbs"]:
                out.writerow([BASELINE_ID, "WBS " + c, "satisfy", r["number"], "SystemRequirement"])
        for v in VERIFICATION:
            out.writerow([BASELINE_ID, v["id"], "verify", v["requirement"], "SystemRequirement"])
    with open(DOCS / "task_ledger.csv", "w", newline="", encoding="utf-8") as handle:
        out = csv.writer(handle)
        out.writerow(["Task", "Role", "Increment", "Objective", "InputBaseline", "ExpectedOutputs", "Reviewer", "State"])
        for t in LEDGER:
            out.writerow([t["task"], t["role"], t["increment"], t["objective"], t["inputs"], t["outputs"], t["reviewer"], t["state"]])


def main():
    validate()
    DOCS.mkdir(exist_ok=True)
    (HERE / "uav_baseline.json").write_text(json.dumps(baseline(), indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    write_tables()
    write_docx()
    write_xlsx()
    write_csvs()
    print(f"Baseline {BASELINE_ID}: {len(NEEDS)} needs, {len(MISSION_REQUIREMENTS)} mission and "
          f"{len(SYSTEM_REQUIREMENTS)} system requirements, {len(WBS['elements'])} WBS elements. Consistency checks passed.")


if __name__ == "__main__":
    main()

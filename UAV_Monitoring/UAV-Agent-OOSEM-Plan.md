# Agent Coordination Plan for UAV Development in PD Quick

Prepared 8 November 2026 | Planning baseline 1 | For stakeholder review

## 1 Purpose and recommended scope

Develop a UAV enabled monitoring and assistance system from the supplied stakeholder stories, using PD Quick as the engineering workspace and OOSEM as the method. ChatGPT agents prepare models, analyses, designs and evidence in assigned engineering roles. Human stakeholders approve needs and operating boundaries; qualified people authorize tests and operational use.

Start with a farm monitoring demonstrator covering escaped cattle and property observation. It offers a coherent outdoor mission while exercising sensing, mission planning, communications, evidence capture and alert delivery. Compare a multirotor and a VTOL fixed wing before selecting an airframe. Neither the storyboards nor this plan establishes a required range, endurance, payload or budget.

A limited Family of Systems is recommended: an outdoor UAV, fixed or wearable sensing, and a shared operator and notification service. The UAV supplies mobile outdoor observations; fixed or wearable devices support indoor continuity and personal assistance. Emergency responders are external actors. This allocation avoids making every need depend on an aircraft being airborne.

This is an execution plan, not a validated aircraft design. PD Quick findings below are based on inspected repository source and documentation at commit 32306b655eba2cea6da5ff1f1b9cad215d5a40da. The application and integrations were not built or exercised for this document. Proposed agent coordination is explicitly distinguished from existing features. No project database was changed.

![Proposed limited Family of Systems](assets/family-of-systems.png)

Figure 1. Proposed system boundary and allocation. Response decisions remain with people and external services.

## 2 Stakeholder needs baseline

Create one source linked need record for each complete scene. Preserve the original wording alongside a normalized need; do not turn a depicted arrest, rescue or successful harvest into a guaranteed UAV outcome. The following identifiers are proposed project identifiers, not existing PD Quick records.

| ID | Storyboard scene | Need to validate and initial allocation |
| --- | --- | --- |
| D01 | Dave arriving and leaving home or car | Family awareness and a record of encounters; fixed and mobile sensing with optional UAV coverage. |
| D02 | Dave threatened near a car in town | Capture an incident and request assistance; personal device first, UAV only in an approved operating setting. |
| D03 | Dave home intrusion | Silent notification and evidence; fixed home sensing first. |
| S01 | Sally predator near livestock | Detect a concerning animal encounter and alert the owner; UAV or fixed observation, with controlled alarm option. |
| S02 | Sally horse theft | Observe an incident, preserve evidence and notify the owner or response service. |
| S03 | Sally poaching | Observe activity on the property and route evidence for human assessment. |
| K01 | Kim nighttime campus threat | User initiated or confirmed assistance, evidence and alarm; personal and campus systems first. |
| K02 | Kim verbal abuse | Record and report an encounter with appropriate consent and access controls. |
| J01 | Jen house fire while away | Receive an alarm and share situational information; existing fixed detection first, optional exterior UAV observation. |
| J02 | Jen property fire | Observe and notify; UAV provides exterior information when operation is authorized. |
| B01 | Bill mother unable to leave bed | Obtain an assistance request and notify a caregiver; wearable or fixed device, without medical diagnosis. |
| B02 | Bill mother locked out | Request assistance and give a responder useful location and context. |
| B03 | Bill mother falls from ladder | Detect or report a possible fall and request help; confirm with the person or caregiver when possible. |
| T01 | Tom escaped cattle | Locate cattle outside the defined enclosure and notify the farmer. |
| T02 | Tom crop distress | Identify areas needing inspection; validate observations against agronomic ground truth. |
| T03 | Tom orchard ripeness | Identify fruit ready for inspection or harvest; validate by crop and growing conditions. |

## 3 Needs analysis before solution design

Exclude Dave scenes 4 through 6, Kim scene 3, and Jen scene 3 because they are incomplete or contain copied placeholder content. Kim scenes 1 and 2 have placeholder titles but substantive narratives and needs, so retain those scenes. Jen's banner refers to a business while her completed scenes describe a home; resolve that mismatch during needs validation.

The needs analyst conducts a structured review with the six stakeholder personas or their representatives. Record the actor, triggering event, desired outcome, current response, environment, frequency, consequences of missed events, nuisance alert tolerance, and evidence needed to decide that the outcome improved. Distinguish stakeholder statements, analyst interpretations, assumptions and decisions.

In PD Quick, begin in Missions. The documented path is Create, Add Mission Element, Stakeholder; then edit the existing element, open Role Properties and create or edit its Needs Statement. Build the operational domain and use cases before allocating functions to the aircraft. Use the OOSEM As Is domain, stakeholder viewpoints, enterprise use cases, To Be domain, mission requirements, measures of effectiveness and fishbone templates as applicable.

| Topic | Question and baseline output |
| --- | --- |
| Mission setting | Property boundaries, terrain, canopy, weather, illumination, people and animals; document an initial operating envelope. |
| Coverage | Area and revisit expectations, continuous versus scheduled monitoring, and gaps tolerated while charging. |
| Response | Named recipient, acknowledgment, escalation, unavailable recipient and communication loss behavior. |
| Information | Useful video, audio or location data; retention, access, consent and evidence transfer needs. |
| Resources | Target cost, staffing, deployment effort, maintenance and training; establish approved trade constraints. |
| Success | Stakeholder measures of effectiveness and measurable engineering performance criteria, each with a test method. |

For T01, an effectiveness measure is the fraction of staged escapes that lead to a timely, actionable farmer acknowledgment. Supporting performance measures include detection probability, false alerts per monitored hour, localization error, notification latency and mission availability. Define denominators, event ground truth, environmental conditions and measurement start and stop points. Thresholds remain TBD until the stakeholder and project engineer approve them; an analyst may propose values but must identify them as provisional.

Gate G1 accepts the scenario set, priorities, boundaries and open questions. It does not approve an airframe. A need is ready for derivation only when its source, owner, outcome and validation approach are recorded.

## 4 Engineering team and decision authority

Agents are role assignments with bounded work packages, not autonomous owners of the program. One person may supervise several roles. Separate authorship from acceptance so an agent does not certify its own result.

| Role | Main responsibility | Review and handoff |
| --- | --- | --- |
| Human sponsor and stakeholders | Priorities, resources, accepted needs and intended use | Approve G1 and operational validation outcomes. |
| Program manager agent | Integrated schedule, dependencies, cost assumptions, risks and review packages | Sends resource conflicts and scope choices to sponsor. |
| Project engineer and chief systems engineer agent | Technical baseline, interfaces, architecture and cross discipline consistency | Accepts specialist integration; sends major trades to human technical authority. |
| Needs and operations analyst agent | Source register, use cases, scenarios, effectiveness measures and CONOPS | Hands reviewed mission baseline to requirements and architecture roles. |
| Requirements and model engineer agent | Requirement quality, derivation, allocation and SysML traceability | Maintains IDs and change impact records with configuration manager. |
| Design engineer agents | Airframe, propulsion, power, payload, communications and ground equipment | Deliver versioned design packages and interface data to analysts. |
| Analysis agents | Mission, energy, structures, sensing, communications and cost studies | Return reproducible results, uncertainty and constraint violations. |
| Software and integration engineer agent | Data interfaces, simulation adapters, flight software integration and alert workflow | Hands tested builds and interface evidence to independent verification. |
| Verification and validation agent | Independent test planning, coverage review, evidence assessment and discrepancy tracking | Recommends gate disposition; does not authorize physical flight. |
| Configuration and tool steward agent | Baselines, model inventory, task ledger, tool ownership and reproducible run records | Publishes accepted revisions and prevents conflicting writes. |

Human technical and test authorities approve release to fabrication, powered tests and flight. ChatGPT agents are engineering assistants; the plan does not place an LLM in a flight control loop. An observer or image classifier reports observations and uncertainty, rather than declaring that a person is a criminal. Alarm behavior is reviewed for people and livestock. Arrest, treatment and fire suppression remain external activities.

## 5 Coordination and shared work rules

Use a project task ledger and versioned artifact store beside PD Quick. The repository's multi agent architecture document describes a proposed coordination pattern; it is not evidence that a persistent scheduler, lease service or automatic agent team is already implemented. Begin with a human supervised coordinator and a single PD Quick writer. Add automation only after its interfaces and recovery behavior are demonstrated.

![Agent work and review flow](assets/agent-workflow.png)

Figure 2. Parallel specialist work is bounded by reviewed inputs, exclusive writes and independent acceptance.

Every work packet contains: task ID and role; objective; source need and requirement IDs; input baseline revision; dependencies; authorized tools and write scope; assumptions; expected files and PD Quick records; acceptance criteria; reviewer; due milestone; and stop conditions. A practical packet name is TASK T01 Mission Trade 003 against baseline B02. IDs should be stored in native records where supported and in the controlled trace manifest otherwise.

The ledger state sequence is Ready, Assigned, Working, Submitted, Reviewed, Accepted or Rework, followed by Baselined. Blocked records name the missing dependency and responsible owner. Only accepted outputs become inputs to downstream baseline work. A changed requirement invalidates affected analyses and tests until reviewed again.

Specialists may read the same frozen baseline and compute in separate run folders. They must not write the same UModel project, Fusion active design, MATLAB workspace or PD Quick database simultaneously. The tool steward grants a task scoped lease, records ownership and releases it after save and readback. An interrupted task leaves a recovery record; it does not silently overwrite another result.

Record command or tool action, tool version, input hashes, units, settings, logs, outputs and result revision for every analysis. A successful process exit or screenshot is insufficient engineering evidence. Distinguish a failed tool execution from a valid result that fails a requirement. Resolve competing proposals through an explicit trade decision, preserving rejected options and rationale.

## 6 OOSEM workflow in PD Quick

Execute OOSEM iteratively. Revisit needs and requirements when trade results reveal infeasibility; do not treat the following sequence as an irreversible waterfall. Each gate has an accountable human reviewer and an evidence package prepared by the agents.

| Phase and gate | PD Quick workspace and modeling work | Exit evidence |
| --- | --- | --- |
| Needs analysis G1 | Missions stakeholders and needs; SysML domain, viewpoint, use case, effectiveness and fishbone diagrams | Reviewed scene register, operational boundary, priorities and unresolved questions. |
| Requirements G2 | Requirements records and documents; scenario activity, input output, context, sequence, black box and state views | Testable requirements with source, rationale, allocation owner and verification method. |
| Logical architecture G3 | Systems hierarchy and SysML blocks, internal interfaces, behavior and constraints | Function allocation independent of a chosen airframe, interface definitions and alternative concepts. |
| Physical architecture G4 | Systems and Designs; CAD models and parameter sets; Analysis linked to Apps | Airframe and sensor trade decision, budgets, margins, interface baseline and preliminary design evidence. |
| Detailed development G5 | Designs, Analysis, WBS and Make; detailed parts, integration and manufacturing planning | Released design package, controlled software configuration and test readiness review. |
| Integration and verification G6 | Analysis, simulation evidence and requirement trace records; Simulations for applicable environments | Requirement results with dispositions, configuration identity and unresolved discrepancies. |
| Operational validation G7 | Missions scenario replay and effectiveness assessment | Stakeholder acceptance or corrective backlog against the original needs. |

In SysML use Create OOSEM Diagram, choose category and catalog entry, set project and title, replace placeholders, then create the native diagram. The documented catalog contains 47 UModel entries and six Excel or fishbone entries. Diagram creation generates starter content; it does not automatically modify PD Quick Systems or prove that a constraint executes.

Use requirement, block definition, internal block, parametric, package, use case, activity, sequence and state diagrams where they answer a review question. Apply deriveReqt, satisfy and verify relations with real identifiers. Do not create diagrams merely to fill a catalog.

## 7 Worked trace for escaped cattle

The example below is a proposed trace, not a populated PD Quick project. It illustrates how the project engineer checks continuity from source to evidence without inventing approved numerical thresholds.

| Record | Proposed content and trace |
| --- | --- |
| SRC T01 | Tom's completed Cattle Monitoring scene; owner wants notification when cattle escape. |
| NEED T01 | Farmer needs actionable awareness of cattle outside the defined enclosure so recovery can begin. |
| UC T01 | Monitor enclosure, detect candidate escape, confirm location, notify, obtain acknowledgment and close event. |
| REQ T01 01 | Within the approved operating envelope, report a confirmed boundary crossing within latency L after ground truth crossing time. L is TBD at G2. |
| REQ T01 02 | Each report contains event time, location with uncertainty, image evidence and delivery state. Acceptance criteria are defined at G2. |
| REQ T01 03 | On loss of observation or delivery capability, indicate unavailable coverage and follow the approved degraded mode. |
| LOG T01 | Observe, estimate location, compare with enclosure, manage event, notify and acknowledge functions. |
| SYS T01 | Allocate observation to UAV or fixed sensor, enclosure processing to onboard or ground compute, notification to shared service. |
| DES T01 | Candidate aircraft, payload, battery, communication link and interface versions tied to the physical baseline. |
| ANA T01 | Coverage, detection, localization, link and energy analyses use the same mission and environmental assumptions. |
| TEST T01 | Staged crossings and noncrossing controls with independent ground truth; verify latency, content and degraded behavior. |
| WBS T01 | Work packages for model, prototype, fixtures, test execution, analysis and corrective actions. |

The requirements engineer creates derivation links from the need and scenario. The design engineer supplies satisfy allocations; the verification engineer supplies verify links and test definitions. The configuration manager also keeps a CSV trace manifest when cross tool links are not supported natively. A link must identify an object and revision, not just a filename.

The analyst models a mission, then the independent reviewer checks units, flight phases, usable energy, reserve policy and uncertainty. A simulated detection result is labeled simulation evidence, not flight evidence. A field test pairs timestamped ground truth with event and delivery logs. Missed events and false alerts remain in the dataset. The farmer validates whether the alert was useful; passing aircraft tests alone cannot close NEED T01.

## 8 Design analysis and integration work packages

Trade concepts against the same approved mission set. Include an outdoor UAV plus fixed sensors, a UAV only concept, and a fixed sensing baseline to expose the value and burden of flight. Evaluate coverage, availability, payload usefulness, noise, deployment effort, lifecycle cost and ability to operate within the approved envelope. Use sensitivity analysis and a Pareto comparison before a weighted score; record who approved the weights.

| Work package | Tool path and expected evidence |
| --- | --- |
| Architecture | UModel native SysML and PD Quick Systems records; reconcile identities and interfaces after edits. |
| Geometry and packaging | Designs with CAD JSON or Fusion; parameter provenance, units, mass properties, payload field of view and maintainability checks. |
| Mission and energy | MATLAB with appropriate licensed capabilities; reproducible mission segments, power assumptions, reserve and coverage calculations. |
| Controls and dynamics | Simulink or Simscape where installed and suitable; model fidelity, stability and failure cases reviewed independently. |
| Aerodynamics | Suitable analytical methods and applicable Datcom studies; document applicability and compare against higher fidelity or measured data where required. |
| Structure | FEMAP and applicable solver or Mystran workflow; loads, materials, constraints, mesh convergence and numerical results. |
| Environment and perception | Unreal project and synthetic scenarios where appropriate; dataset provenance, scene limitations and separate real world validation. |
| Mission integration | ArduPilot SITL, followed by controlled hardware and flight tests; configuration matched to trajectory and comparison runs. |

PD Quick source includes a UAV pipeline factory describing export, optional terrain, trajectory, ArduPilot SITL, simulated comparison, a manual Mission Planner flight stage and flown comparison. Factory code is not proof that every stage is available in the UI or operationally connected. Qualify each adapter with a small known case before scheduling dependent work. A skipped optional terrain stage produces a warning; a failed terrain stage must be treated as a blocking issue in that sequence.

For farm monitoring, prioritize camera suitability and observation quality before adding thermal or multispectral payloads. Crop condition and ripeness require labeled field samples, seasonal variation and independent checks. An attractive synthetic scene does not establish detection performance. Personal and indoor scenarios should first be tested using the nonairborne members of the Family of Systems.

## 9 Data discipline and verification

Maintain three separately checked saves: the PD Quick record, the native tool model, and the evidence package. UModel changes do not automatically synchronize PD Quick Systems. A Fusion cloud project is distinct from its native design. Record both identities where relevant. Shared systems can affect several projects, so change impact review must include linked projects.

![Engineering trace and evidence chain](assets/traceability.png)

Figure 3. Proposed trace chain with revision controlled evidence and change impact feedback.

For each requirement, record verification method, level, configuration, preconditions, procedure, measured quantity, acceptance threshold and result. Methods include inspection, analysis, demonstration and test. Simulation supports early verification but does not replace validation of the real sensing environment, human workflow or operational aircraft.

Plan nominal and degraded cases: low illumination, occlusion, ambiguous events, disconnected communications, exhausted availability, unavailable recipient, stale maps, incorrect timestamps and interrupted tool runs. Select scenarios from the needs and architecture, with severity based prioritization. Do not infer numerical reliability from a few successful demonstrations.

At G6, the independent verification role reports pass, fail, blocked or not run for each planned check. Preserve raw evidence and discrepancies. At G7, stakeholders assess the original outcomes using representative scenarios and realistic response arrangements. External responder acknowledgment is demonstrated through an agreed test interface or exercise, not by sending live emergency alerts during development.

The release manifest contains baseline ID, source commit, tool versions, parameter files, artifact hashes, trace matrix, open issues and approved deviations. Keep personal recordings restricted and separate from general engineering examples. Use synthetic or consenting participants for development scenarios. Human reviewers decide the operating rules, retention and access arrangements for the intended deployment before trials.

## 10 Delivery sequence and management cadence

Use milestone based planning until team capacity, budget and tool access are known. The sequence below is an initial backlog, not a committed schedule. The program manager estimates effort and critical path after the capability check and G1 scope decision.

| Increment | Work and dependency | Demonstrable completion |
| --- | --- | --- |
| A Workspace readiness | Inventory PD Quick, licenses, adapters and native save paths | One sample record, diagram and deterministic analysis saved and read back. |
| B Needs baseline | Normalize 16 scenes and validate priority farm missions | G1 package accepted; exclusions and ambiguities recorded. |
| C Requirements and concepts | Derive requirements, interfaces and candidate allocations | G2 and G3 packages; test methods and unresolved thresholds visible. |
| D Preliminary design | Compare aircraft and sensing concepts using common mission assumptions | G4 trade decision supported by reproducible analyses and uncertainty. |
| E Integrated simulation | Connect mission, geometry, dynamics, sensing and notification test interfaces | End to end T01 scenario in simulation with traceable logs. |
| F Prototype and controlled tests | Detailed design and integration after readiness approval | G5 then G6 evidence; defects tracked to closure or accepted disposition. |
| G Stakeholder validation | Representative farm scenario after flight and test authorization | G7 effectiveness review and decision on deployment or redesign. |
| H Family extension | Reuse reviewed services for property, personal and assistance use cases | Separate validated operating envelopes; no automatic inheritance of aircraft suitability. |

Each working cycle starts with dependency and baseline checks. Specialists submit artifacts with a short result and uncertainty statement. The project engineer resolves interface conflicts; verification reviews evidence; configuration management publishes accepted outputs. Hold regular risk and change reviews and a gate review only when its evidence package is ready.

Track orphan needs, requirements without tests, stale analyses, unresolved interfaces, blocked tasks, defects by severity, and accepted versus planned work. Do not equate number of agent messages or diagrams with engineering progress. Escalate contradictory needs, infeasible margins and unavailable tools early; a blocked integration can proceed with a labeled stub for isolated testing, but the stub cannot count as final evidence.

## 11 Risks and first decisions

| Risk or uncertainty | Planned action and owner |
| --- | --- |
| One aircraft stretched across unrelated missions | Chief SE maintains explicit allocations and operating envelopes; sponsor approves farm first scope. |
| Continuous coverage exceeds flight availability | Mission analyst models charging, service and missed coverage; compare fixed sensing support. |
| Incorrectly confident event classification | Perception analyst reports uncertainty and ground truth results; operator workflow supports review. |
| Weak link or unavailable recipient | Integration engineer tests buffering, delivery state and escalation with agreed recipients. |
| Indoor and care needs misallocated | Needs analyst validates nonairborne alternatives with the affected person and caregiver. |
| Concurrent tool edits or hidden state | Tool steward enforces single writer leases, isolated runs, save and readback checks. |
| Claimed integration exceeds implementation | Project engineer qualifies each tool adapter and records manual fallback work. |
| Scope and acceptance targets remain unclear | Program manager maintains decision log with owner and milestone; no silent numerical assumptions. |

First decisions are the pilot property and stakeholder representative; which T01 and property observation outcomes define the MVP; operating conditions; budget and staffing; response recipients; acceptable missed event and nuisance alert rates; and the measures to be accepted at G2. These questions are work items for needs analysis, not reasons to postpone preparing the plan.

The first executable team assignment is to create a source register and T01 mission package, inventory tool readiness, and demonstrate one round trip between a PD Quick record, a native diagram and a deterministic analysis output. The project engineer then convenes G1. Broader airframe design proceeds only against that reviewed baseline.

## 12 Capability and source manifest

| Capability | Evidence status for this plan |
| --- | --- |
| Missions stakeholders and needs workflow | Documented in the local OOSEM development guide; not live tested here. |
| OOSEM template catalog and native starter content | Documented implementation and template behavior; catalog counts are documentation based. |
| Requirements, Systems, Designs, Analysis, Simulations, WBS and Make | Documented workspace roles; field availability must be checked in the installed build. |
| Fusion and structural tooling services | Source present; installed adapters, licenses and numerical operation not verified here. |
| UAV staged pipeline | Factory source inspected; not evidence of a running end to end coordinator. |
| Agent ledger, leases and role routing | Proposed workflow, informed by the repository architecture proposal. |
| Cross tool trace manifest and gate packages | Proposed project artifacts; no claim of automatic synchronization. |

Repository evidence is F:\PDQuick at commit 32306b655eba2cea6da5ff1f1b9cad215d5a40da. Relevant sources are README.md; MULTI_AGENT_ARCHITECTURE.md; docs/OOSEM_SYSTEM_DEVELOPMENT_GUIDE.md; docs/OOSEM_DIAGRAM_TEMPLATES.md; DWM.Shared/Tooling/ProjectPipeline.cs; DWM.Shared/Tooling/ToolRun.cs; and the Fusion stage, parameter and document services, Mystran runner, FEMAP post processor and WorldPackageCli Fusion check inspected during preparation. Source presence is distinguished throughout from runtime qualification.

Storyboard sources are the six user supplied images: Dave f1b5b937, Sally b46df680, Kim 37c69331, Jen da376920, Bill 055a39bc and Tom b702801a, each from its codex clipboard PNG attachment. These identifiers refer to the filename prefixes following codex-clipboard. Only complete scenes were used. The proposed diagrams in this document are original planning illustrations derived from the accepted scene content; they are not PD Quick screenshots or existing product architecture.

D:\DreamWorldMaker\Images was inspected as an optional source. Original workflow and architecture diagrams were selected because they communicate the plan more directly than the available small icons. No incomplete storyboard scene is reproduced. The Markdown and Word versions contain the same plan content, with the images embedded in Word and stored in the accompanying assets folder for Markdown.

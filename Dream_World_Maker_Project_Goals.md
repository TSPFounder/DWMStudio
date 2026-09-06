# Dream World Maker — Project Goals

## Executive Summary: Experiments in Optimal Living

Dream World Maker (DWM) is a collaborative world-building and simulation project in which people explore how communities can provide what their members need for a happy and sustainable life. Users shape their communities around their own needs, hopes, abilities, and surroundings, and discover how cooperation, engineering, and shared knowledge can turn those ideas into workable systems.

The project brings together three essential capabilities: a simulated world in which communities can be formed and their choices explored; a Local Exchange Trading System (LETS), founded on a reliable mutual-credit ledger; and an open-source library of useful items, infrastructure, and the affordable manufacturing tools needed to make them.

DWM's intended outcome extends beyond the simulation. Designs and lessons developed in the virtual communities should help people build, maintain, and improve real communities. The game makes participation approachable, while the engineering workflow provides a path from an idea to a documented and tested design.

## Mission Statement

**Dream World Maker attempts to simulate communities that users form to provide what they need for a happy and sustainable life. It enables people to explore ways of living together, exchange goods and services through a ledger-based LETS economy, and collaboratively develop and share open-source designs for the items, infrastructure, and affordable tools that make those communities possible.**

## 1. Let People Define and Build Their Communities

DWM aims to give people a practical setting in which to ask: What do we need to live well, what can we provide ourselves, and what can we accomplish together?

Community needs include food, water, shelter, energy, clothing, transportation, communications, and useful tools. A happy life also requires belonging, meaningful participation, learning, creativity, and time to enjoy what a community creates. The purpose of production and exchange is to support those needs.

Users should be able to explore different arrangements rather than follow a single prescribed model of an ideal community. They can compare choices about settlement patterns, shared facilities, resource use, productive activities, and relationships with other communities. Nontechnical participants contribute needs and ideas; technical participants help develop the systems that can satisfy them.

The present demonstration uses five communities—Mountain, Hillside, Valley, Suburb, and City—to make these relationships understandable. These are the starting example for a broader platform in which users can shape communities of their own. The repository's current MVP scope is a local, single-player demonstration; user-created shared worlds and multiplayer persistence belong to the longer-term vision. [1]

## 2. Make Cooperation and Sustainable Self-Reliance Work Together

DWM explores self-reliance across an interconnected network. Different communities have different resources, skills, and opportunities, so each can contribute something that others need.

The Mountain turbine story illustrates the principle: engineering designs come from Hillside, food from the Valley, skilled labor from the Suburb, and manufactured parts and control software from the City. Their combined contributions make a project possible that one community could not complete alone. [2]

The wider goal is to help users understand and improve these relationships:

- Produce food and energy close to where they are needed when practical.
- Share specialized skills, equipment, and facilities across communities.
- Repair, reuse, and recover materials so that one activity's leftovers become another's inputs.
- Examine resource limits and competing needs, including land, labor, energy, and maintenance effort.
- Reduce recurring dependence on outside purchases by developing useful local productive capacity.

Sustainability should emerge from the behavior of the simulated systems: what they consume, what they produce, how long they last, and how they respond when a resource or service becomes unavailable. Users should be able to see trade-offs and improve their choices through repeated experiments.

## 3. Support Exchange Through a LETS System Founded on the Ledger

The economic foundation is a **Local Exchange Trading System (LETS)** implemented through a **mutual-credit ledger**. Its purpose is to let communities exchange useful goods and services without requiring a pre-existing stock of conventional money for every internal transaction. [3]

The project's unit of account is the **Stone (St)**, with a stated labor reference of **one hour = one Stone**. At an exchange, the buyer's balance decreases and the seller's balance increases by the same amount. A trade for 10 Stones therefore records a change of −10 for the buyer and +10 for the seller. The net change across the network is zero. A negative balance represents a mutual-credit position within the network; it is an expected part of the model. Interest and speculative currency mechanics are excluded from the stated design. [1][3]

The ledger is the authoritative record of those exchanges. Its history must remain auditable, corrections must preserve that history, and each accepted trade must settle only once. These properties matter because communities need to trust the record of their contributions and obligations. The repository documents an append-only transaction model and safeguards against repeated settlement. [3][4]

DWM also separates internal mutual credit from external conventional currency through the **Dollar Vault**. Stones can coordinate available labor and production within the network; purchases from outside it still consume finite outside funds. This distinction makes dependence on imported materials, equipment, and services visible and gives communities a reason to develop alternatives. [3][4]

The long-term social goal is broad participation in meeting community needs. The current MVP ledger records exchanges at community level; the full treatment of individual membership and accounts is a later extension. [3]

## 4. Build an Open-Source Library of Items People Develop for Their Needs

A central goal is a growing public library of **open-source items and systems developed by people to meet their own and their communities' needs**. A useful design created for one community should become available for others to use, adapt, improve, and contribute back.

The library should cover everyday items as well as larger systems: household goods, building components, food-production equipment, energy systems, water infrastructure, transport equipment, communications devices, control software, and manufacturing machinery. Its value lies in preserving enough knowledge to reproduce and maintain an item.

For a design intended to be built, the desired library package includes:

- The need it addresses, its requirements, and its intended operating conditions.
- Editable design files and accessible exchange formats.
- Drawings, dimensions, bills of materials, and suitable material choices.
- Manufacturing, assembly, operation, maintenance, and repair information.
- Relevant calculations, simulation results, test evidence, and known limitations.
- Version history, contributor attribution, and explicit open-source licensing.

Earlier project discussions establish the intended flow: develop and test designs in the DWM_Dev engineering environment, make approved assets available in DWM, and publish the approved open-source designs in a public community GitHub repository for others to use.

This is an enduring project goal. The current MVP demonstrates a limited engineering path centered on the wind turbine; the full cross-system design library remains broader than the MVP's scope. Commercial software or visual assets used during development retain their own licensing conditions; they do not automatically become part of the open-source design collection. [1]

## 5. Develop Affordable Open-Source Tools for Making Those Items

Communities need access to the means of making, modifying, and repairing their designs. DWM therefore includes the development and use of **affordable, open-source manufacturing tooling**, designed for low construction cost, maintainability, and practical reproduction.

The intended tooling collection includes:

| Tool | Intended contribution to community production |
| --- | --- |
| CNC routers | Machine components, jigs, fixtures, and molds, including molds for turbine blades. |
| CNC lathes | Produce turned parts such as shafts, bushings, spacers, and fittings. |
| Laser cutters and engravers | Make suitable-material profiles, templates, patterns, markings, and other fabrication aids. |
| 3D printers | Produce prototypes, useful parts, jigs, fixtures, and patterns for casting. |
| Furnaces for making metal castings | Turn suitable metal feedstock, including recovered material, into cast components for subsequent finishing. |
| Milling machines and drill presses | Finish castings and produce accurately located surfaces and holes. |
| Brake presses, sheet-metal brakes, and arbor presses | Form sheet-metal parts and support pressing and assembly operations. |

The tooling designs themselves belong in the shared open-source library. They should be documented so communities can build, understand, service, and improve them, with costs and capabilities made explicit.

An important goal is to develop manufacturing capacity incrementally: a printer produces a casting pattern; a foundry makes the casting; machine tools finish it; and the finished components help build another useful machine. The repository records this kind of manufacturing progression as a post-MVP direction, including a foundry-to-machine-tool sequence, CNC routers, printers, laser tooling, and a CNC lathe. [1]

The aim is a common set of designs and compatible parts that several communities can use. Shared tooling and interchangeable components reduce repeated design work and make repairs and improvements easier to spread. Affordability includes the cost of maintaining and reproducing an item, as well as its first construction. This is the direction of the turbine story's ending: use the immediate repair to gain time and knowledge, then develop smaller machines and the shared tools to make them. [2]

## 6. Connect Community Needs to Engineering and Realization

DWM seeks to connect the social question—what people need—with the engineering question—what can be designed and made to provide it.

The intended development cycle is:

1. **Identify and prioritize a need** within a community.
2. **Define requirements and compare approaches**, including resource demands and trade-offs.
3. **Develop the design** using appropriate modeling, CAD, analysis, and simulation tools.
4. **Test and document the results**, including assumptions and remaining limitations.
5. **Promote a verified version into DWM** to explore its use within the community system.
6. **Share the open-source design** and improve it as further simulation or physical experience becomes available.

The major project components serve complementary purposes:

| Component | Role in the project |
| --- | --- |
| **DWMStudio** | The engineering workbench that organizes projects, external tools, design artifacts, runs, and exported world packages. |
| **DWM_Dev** | The development and testing environment, including the Unreal project and engineering models used to prove the design-to-simulation path. |
| **DWM** | The intended production simulation in which users experience and operate communities using promoted assets and systems. |
| **Public open-source design library** | The reusable collection of community-developed items, tooling designs, documentation, and supporting evidence. |

DWMStudio's documented integrations include CAD, MATLAB/Simulink, and structural-analysis tools. These support the larger objective of choosing suitable engineering methods for different systems. The platform should also accommodate work performed manually in tools without automation interfaces. [5]

Simulation detail should match the question being explored. Early community experiments may use simple Unreal assets with little or no engineering complexity. Designs being considered for physical realization need more detailed models and evidence. The current wind-turbine work demonstrates this progression while documenting the limits of its geometry and engineering models. Passing a simulation or software verification gate is one step toward a buildable design; physical validation remains part of real-world realization. [1][6][7]

## 7. What Success Should Mean

DWM succeeds as its users become better able to:

- Form communities around their own definitions of a happy and sustainable life.
- Understand what their communities need, produce, consume, and depend upon.
- Coordinate useful contributions through a trustworthy LETS ledger.
- Design and test improvements before committing scarce real-world resources.
- Make and repair useful items with affordable, reproducible tooling.
- Share designs so that one community's progress benefits others.
- Carry appropriate designs and lessons from the simulated world toward real-world realization.

These are the project's enduring goals. The five-community demonstration and wind-turbine workflow provide an initial way to show how they fit together.

## Basis and Sources

Prepared **September 6, 2026**, from Henry Wayland's current direction, available prior Dream World Maker conversation context, and selected documents in **TSPFounder/DWMStudio** and **TSPFounder/DWM_Dev**. Earlier conversations supply the community vision, “Experiments in Optimal Living” title, public design-library objective, and affordable maker-tooling goals. Repository documents supply the MVP boundaries, ledger design, engineering workflow, and manufacturing storyline.

This document expresses project goals; it is not a claim that every described capability is already implemented. Dated decisions in the repository's scope log supersede older statements where they conflict. Implementation statements below reflect the reviewed documentation rather than a fresh execution of the software.

Repository snapshots reviewed: DWMStudio `22312a4dd68f6855247429e8ea49df4cfc52cbb9`; DWM_Dev `a30f71c9f88085d20ca99e3ed38073c62cc12c11`.

1. [DWMStudio — SCOPE.md](https://github.com/TSPFounder/DWMStudio/blob/22312a4dd68f6855247429e8ea49df4cfc52cbb9/SCOPE.md), including the MVP scope and dated manufacturing and turbine decisions; [DWM_Dev — README.md](https://github.com/TSPFounder/DWM_Dev/blob/a30f71c9f88085d20ca99e3ed38073c62cc12c11/README.md).
2. [DWMStudio — The Mountain's Turbine](https://github.com/TSPFounder/DWMStudio/blob/22312a4dd68f6855247429e8ea49df4cfc52cbb9/DWM_MVP_Book.md), particularly the cooperative repair, cost verdict, shared tooling, and common-design narrative. Story events illustrate project intent rather than independently verified implementation.
3. [DWMStudio — Economy Schema Design Spec](https://github.com/TSPFounder/DWMStudio/blob/22312a4dd68f6855247429e8ea49df4cfc52cbb9/ECONOMY_SCHEMA_SPEC.md).
4. [DWMStudio — Stone Ledger Economy Reference](https://github.com/TSPFounder/DWMStudio/blob/22312a4dd68f6855247429e8ea49df4cfc52cbb9/DEMO_ECONOMY.md).
5. [DWMStudio — External Tool Integration Design](https://github.com/TSPFounder/DWMStudio/blob/22312a4dd68f6855247429e8ea49df4cfc52cbb9/TOOLING.md).
6. [DWM_Dev — Fusion Wind Turbine Model Documentation](https://github.com/TSPFounder/DWM_Dev/blob/a30f71c9f88085d20ca99e3ed38073c62cc12c11/Models/Fusion/MVP_WindTurbine/README.md).
7. [DWM_Dev — Simulink Wind Turbine Model Documentation](https://github.com/TSPFounder/DWM_Dev/blob/a30f71c9f88085d20ca99e3ed38073c62cc12c11/Models/Simulink/MVP_WindTurbine/README.md).

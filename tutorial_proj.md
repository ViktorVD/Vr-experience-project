# Dualist VR: Reproduceerbare AI-gestuurde Zwaardgevechten

## 1. Inleiding

**Dualist VR: AI-gestuurde Zwaardgevechten in Virtual Reality**
In dit project werd een functionele VR-simulatie ontwikkeld waarbij een menselijke speler het kan opnemen tegen een AI-gestuurde tegenstander in een één-tegen-één zwaardgevecht. De tegenstander maakt gebruik van Reinforcement Learning (via Unity ML-Agents) om zelfstandig aanvals- en verdedigingsstrategieën aan te leren zonder gebruik te maken van voorgeprogrammeerde scripts. Dit resulteert in een dynamisch en onvoorspelbaar gevecht waarin timing, positionering en schildgebruik essentieel zijn om als winnaar uit de virtuele arena te stappen.

Deze tutorial dient als uitgebreide gids om ons project vanaf de basis te reproduceren. Tegen het einde van dit document zal de lezer inzicht hebben verworven in de benodigde softwarepakketten en configuraties, het gedrag en de structuur van de virtuele omgeving, en de specifieke observaties, acties en beloningen die cruciaal zijn voor het effectief trainen van de vechtende AI-agent. Tevens worden de trainingsresultaten geanalyseerd.

---

## 2. Methoden

### 2.1. Installatie en Benodigdheden

Voor dit project werd gebruik gemaakt van de volgende software- en package-versies:

- **Unity Editor**: Versie 6000.2.7f2
- **ML-Agents**: Versie 2.0.2
- **XR Interaction Toolkit**: Versie 3.2.2
- **Oculus XR Plugin**: Versie 4.5.4
- **OpenXR Plugin**: Versie 1.15.1

Daarnaast zijn de volgende gratis Unity Asset Store pakketten geïmporteerd:
- [Low Poly 3D Wooden Shield Pack](https://assetstore.unity.com/packages/p/low-poly-3d-wooden-shield-pack-359602)
- [Free Low Poly Swords - RPG Weapons](https://assetstore.unity.com/packages/p/free-low-poly-swords-rpg-weapons-198166)
- Arena omgeving (Eigen implementatie / Standaard primitive objecten)

### 2.2. Verloop van de Simulatie

De simulatie start in een virtuele arena waar de speler en de AI-agent recht tegenover elkaar verschijnen, beiden uitgerust met een zwaard en een schild. Het doel van het spel is om de hitpoints (HP) van de tegenstander tot nul te herleiden door middel van succesvolle zwaardaanvallen, terwijl eigen schade voorkomen wordt door slagen te blokkeren of te ontwijken. Zodra een deelnemer al zijn HP verliest, wordt er één punt toegekend aan de winnaar. De match wordt gespeeld in een "best of 5" format; wie als eerste drie rondes wint, wordt via een eindscherm uitgeroepen tot winnaar. 

Dit is in lijn met het initiële projectvoorstel (de one-pager), waarbij een Adversarial Self-Play model werd vooropgesteld. Er is gedurende het traject niet fundamenteel afgeweken van het originele concept.

### 2.3. Beschrijving van Objecten en Gedragingen

- **De Speler (VR)**: Bestuurd door de fysieke bewegingen van de gebruiker. De headset regelt het zicht en de beweging, terwijl de motion controllers direct gekoppeld zijn aan de beweging van het zwaard (rechterhand) en schild (linkerhand).
- **De AI Agent**: Een zelfstandige entiteit aangedreven door ML-Agents. Het object navigeert door middel van discrete acties en kan verschillende voorgedefinieerde aanvals- en blokkeeranimaties afspelen.
- **Het Zwaard**: Heeft een hitbox die schade toebrengt bij een botsing (collision) met het lichaam van de tegenstander, mits de aanval actief is.
- **Het Schild**: Dient ter obstructie van de inkomende zwaardslagen. Wanneer het zwaard van de tegenstander het schild raakt, wordt de aanval gepareerd en volgt er geen schade.

### 2.4. Reinforcement Learning: Observaties, Acties en Beloningen

Om de AI succesvol te trainen, werd de volgende set aan inputs (observaties) en outputs (acties) gedefinieerd, gekoppeld aan een beloningssysteem (rewards).

#### Observaties (Inputs)
De agent neemt continue de staat van zijn omgeving waar:
1. **Afstand**: De relatieve afstand tot de tegenstander, genormaliseerd tussen 0 en 1.
2. **Posities (Relatief)**: 
   - De relatieve vector richting de tegenstander.
   - De relatieve positie van het zwaard van de tegenstander.
   - De relatieve positie van het schild van de tegenstander.
3. **Status Tegenstander**: Boolean-waarden (0 of 1) die aangeven of de tegenstander op dat moment aanvalt of blokkeert, inclusief de richting van diens blokkade.
4. **Eigen Status**: Boolean-waarden die de eigen actieve staat (aanvallend, blokkerend, afkoelingsperiode) en de actuele hitpoints (HP) van beide spelers vertegenwoordigen.

#### Acties (Outputs)
De agent maakt gebruik van Discrete Actions over meerdere branches:
- **Branch 1: Bewegen**: (0) Stilstaan, (1) Vooruit, (2) Achteruit, (3) Naar links, (4) Naar rechts.
- **Branch 2: Aanvallen**: (0) Geen aanval, (1) Overhead Strike (verticale slag), (2) Side Swing (horizontale slag), (3) Stab (steekaanval).
- **Branch 3: Blokkeren**: (0) Niet blokkeren, (1) Centraal blokkeren, (2) Linksboven blokkeren, (3) Rechtsboven blokkeren.
- **Branch 4: Roteren**: (0) Niet roteren, (1) Links roteren, (2) Rechts roteren.

#### Beloningen (Rewards & Penalties)
De *reward function* is zorgvuldig gekalibreerd om gewenst gedrag te stimuleren:
- **Positieve Beloningen (+)**:
  - Een succesvolle treffer met het zwaard (+0.8).
  - De hitpoints van de tegenstander reduceren tot nul (+1.0).
  - Voldoende afstand bewaren om tactisch overzicht te houden (+0.01).
  - Afwisselen van aanvalstypes ter bevordering van variatie (+0.01).
  - Ontwijken van inkomende aanvallen door naar achteren te stappen (+0.005).
  - Continu naar de tegenstander gericht blijven (+0.005).
- **Negatieve Straffen (-)**:
  - Eigen HP gereduceerd tot nul (-1.0).
  - Falende aanvallen (aanvallen die missen) om spammen te voorkomen (-0.02).
  - Dezelfde aanval constant herhalen (-0.02).
  - Onnodig of willekeurig blokkeren (-0.001).
  - Te dicht ('face hugging') of een ongunstige afstand ten opzichte van de vijand (-0.005 / -0.004).

---

## 3. Resultaten

*(Let op: Hieronder dienen de uiteindelijke screenshots van Tensorboard geplaatst te worden. Onderstaande grafieken dienen als voorbeeld voor de opmaak.)*

### 3.1. Tensorboard Grafieken

![Cumulative Reward Training](images/placeholder_reward.png)  
*Grafiek 1: Cumulative Reward over de totale trainingsstappen (zonder smoothing).*

![Episode Length](images/placeholder_length.png)  
*Grafiek 2: Gemiddelde duur van een episode (Episode Length).*

### 3.2. Beschrijving en Waarnemingen

Uit de trainingsdata (Grafiek 1) valt duidelijk af te leiden dat de *Cumulative Reward* gedurende de eerste miljoen stappen aanzienlijk schommelde, waarna het model een stabielere, opwaartse trend begon te vertonen. Dit duidt erop dat de agent initieel willekeurige acties (exploration) uitvoerde, wat resulteerde in vele gemiste aanvallen en strafpunten. Na verloop van tijd leerde de policy echter dat het combineren van blokkeren en afwisselend aanvallen de meest belonende strategie was (exploitation).

Een opvallende waarneming tijdens de trainingsfase was dat de agent rond de 2 miljoen stappen in een lokaal optimum verzeild raakte: hij leerde dat continu achteruitlopen strafpunten voor het missen van aanvallen vermeed. Dit probleem werd verholpen door de straf voor 'te ver weglopen' aan te scherpen en het correct in de richting van de vijand kijken actiever te belonen, waarna we zagen dat de *Episode Length* (Grafiek 2) drastisch daalde omdat gevechten sneller en agressiever werden beslecht.

---

## 4. Conclusie

In dit project is met succes een adaptieve AI-gladiator gerealiseerd voor een VR-gevechtssimulator, die door middel van reinforcement learning zelfstandig gevechtstactieken heeft aangeleerd. De uiteindelijke AI-agent toont competitief gedrag, kan inkomende slagen blokkeren en past een gevarieerd aanvalsarsenaal toe. Persoonlijk beschouwen wij het bereikte resultaat als een sterke demonstratie van de meerwaarde van zelflerende agenten ten opzichte van klassiek geprogrammeerde vijanden in interactieve VR-games; het levert een veel natuurlijkere en onvoorspelbaardere ervaring op. Voor de toekomst zouden we het project nog willen verbeteren door het toevoegen van meer geavanceerde Inverse Kinematics (IK) voor vloeiendere arm-animaties van de AI, en het trainen van meerdere agent-persoonlijkheden (bijv. agressief versus defensief).

---

## 5. Bronvermelding

*Unity Technologies. (2024). ML-Agents Toolkit (Versie 2.0.2) [Software]. Geraadpleegd via https://github.com/Unity-Technologies/ml-agents*

*Cursusmateriaal en Instructievideo's Artificial Intelligence. (2024). AP Hogeschool Antwerpen.*

*Obelix Tutorial. (2024). ML-Agents implementatiegids. [Intern Cursusdocument]. AP Hogeschool Antwerpen.*
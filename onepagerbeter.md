# Dualist VR – VR Duel Simulator met Adaptieve AI

## Team
Het project wordt uitgevoerd door:
- Louis Boulez  
- Viktor Van Deun  

---

## Titel
**Dualist VR – VR Duel Simulator met Adaptieve AI**

---

## Draaiboek (Logica van de VR-simulatie)

1. De speler start de applicatie en klikt op de startknop om het spel te beginnen.  
2. De speler wordt gespawned in een virtuele arena, oog in oog met een AI-gestuurde gladiator.  
3. De speler wordt uitgerust met een zwaard en een schild en heeft de mogelijkheid om zich vrij (fysiek of via de controller) binnen de arena te verplaatsen.  
4. De speler kan diverse acties ondernemen:
   - Aanvallen uitvoeren met het zwaard (snijden of steken)  
   - Zichzelf verdedigen door inkomende slagen te blokkeren met het schild  
   - Strategisch positioneren door in de arena te bewegen en aanvallen te ontwijken  
5. De AI-tegenstander observeert continu de speelomgeving:
   - De exacte positie en rotatie van de speler  
   - De bewegingen en posities van het zwaard en het schild van de speler  
   - De relatieve afstand tussen speler en AI  
6. Op basis van deze real-time waarnemingen selecteert de AI via zijn neuraal netwerk de meest optimale acties (aanvallen, verdedigen of herpositioneren).  
7. De speler en de AI interageren continu tijdens het gevecht.  
8. Zodra een speler of de AI succesvol geraakt wordt:
   - Verliest de getroffen partij een vooraf bepaald aantal hit points (HP).  
9. Het gevecht of de ronde gaat door totdat de HP van één van de deelnemers tot 0 of lager zakt.  
10. De winnaar van de betreffende ronde ontvangt 1 punt.  
11. Het spel verloopt in rondes en gaat door totdat één van de deelnemers 3 punten (rondes) heeft behaald.  
12. Aan het einde krijgt de speler een eindscherm te zien dat de winnaar van de match toont en de mogelijkheid biedt om opnieuw te spelen.

---

## Meerwaarde van AI en het Type AI-agent

Wanneer de tegenstander geprogrammeerd zou zijn op basis van vaste regels of scripts (bv. Finite State Machines), zou dit leiden tot de volgende nadelen:
- De tegenstander wordt na verloop van tijd voorspelbaar.  
- De speler zal snel de vaste patronen herkennen, waardoor de uitdaging verdwijnt.  
- Als de scripts te perfect zijn, wordt de tegenstander oneerlijk en frustrerend om tegen te spelen.

Door het implementeren van een **Adversarial Self-Play AI-agent** via ML-Agents, leert de AI door herhaaldelijk tegen zichzelf en willekeurige policies te spelen. Dit resulteert in een aantal belangrijke voordelen:
- De AI ontwikkelt dynamische en steeds beter wordende strategieën op basis van trial en error (Reinforcement Learning).  
- Het zorgt voor een meer realistische, responsieve en menselijke uitdaging.  
- Elke match verloopt anders, wat zorgt voor een hoge mate van variatie en replayability, resulterend in een veel boeiendere spelervaring.

---

## Waarom Virtual Reality?

Virtual Reality voegt een cruciale laag van immersie toe die met klassieke beeldschermen niet gerealiseerd kan worden. Voor dit specifieke duel-concept is VR uitermate geschikt omdat:
- De speler fysiek aanwezig is en de schaal van de arena en de tegenstander kan inschatten.  
- Reacties en gevechtsbewegingen gebaseerd zijn op werkelijke fysieke handelingen (1-op-1 mapping van de armen) in plaats van abstracte knoppencombinaties.  
- De speler actief zijn hoofd en lichaam moet gebruiken om de acties van de tegenstander in het oog te houden, in te schatten en te ontwijken.  
- Het dragen en hanteren van een zwaard en schild via VR-controllers zeer natuurlijk en intuïtief aanvoelt, wat de *presence* aanzienlijk verhoogt.  

Hierdoor wordt de combat-ervaring enorm intens en realistisch, een fundamentele pijler voor een geslaagde gevechtssimulatie.

---

## Interacties in de VR-omgeving

De kern-interacties binnen het spel zijn uiterst fysiek en gebeuren via motion controls:
- **Aanvallen**: De speler hanteert het zwaard met één hand en voert fysieke zwaaibewegingen uit om schade toe te brengen.  
- **Verdedigen**: De speler gebruikt het schild in de andere hand en positioneert dit fysiek in de baan van een inkomende aanval om deze te pareren.  
- **Navigatie**: De speler kan rondlopen met de thumbsticks (of via room-scale VR fysiek stappen) om afstand te creëren of de kloof te dichten.  
- **Tactiek**: De speler moet actief richten, de acties van de AI analyseren, openingen in de verdediging spotten en zijn eigen slagen perfect timen.

Deze combinatie van aanval, verdediging en ruimtelijke positionering staat garant voor interactieve en dynamische gameplay waarbij reflexen en constante besluitvorming centraal staan.
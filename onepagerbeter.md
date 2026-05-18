# Dualist VR  – VR Duel Simulator met Adaptieve AI

## Team
Het project wordt uitgevoerd door:
- Louis Boulez  
- Viktor Van Deun  
---

## Titel
**Gladiator – VR Duel Simulator met Adaptieve AI**

---

## Draaiboek (logica van de VR-simulatie)

1. De speler start de applicatie en klikt op de startknop.  
2. De speler wordt in een virtuele arena geplaatst, samen met een AI-gestuurde tegenstander.  
3. De speler krijgt controle over een zwaard en een schild en kan vrij bewegen binnen de arena.  

4. De speler kan verschillende acties uitvoeren:
   - Aanvallen met het zwaard  
   - Verdedigen met het schild  
   - Zich positioneren door rond te bewegen  

5. De AI-tegenstander observeert continu:
   - De positie van de speler  
   - De bewegingen van het zwaard en schild  
   - De afstand tussen speler en AI  

6. Op basis van deze informatie kiest de AI in real time zijn acties (aanval, verdediging, positionering).  

7. De speler en de AI herhalen deze interacties voortdurend tijdens het gevecht.  

8. Wanneer een speler of de AI geraakt wordt:
   - Verliest de getroffen partij een aantal hit points (HP)  

9. Het gevecht gaat door totdat de HP van één van beide spelers 0 bereikt.  

10. De winnaar van de ronde krijgt 1 punt.  

11. Het spel loopt door totdat één van de spelers 3 punten heeft behaald.  

12. De speler krijgt een eindscherm te zien met de winnaar.

---

## Meerwaarde van AI + type AI-agent

Zonder AI zou de tegenstander gebaseerd zijn op vaste regels of scripts. Dit zou ervoor zorgen dat:
- De tegenstander voorspelbaar wordt  
- De speler snel een patroon kan herkennen en altijd kan winnen  
- Of net dat de tegenstander “perfect” speelt en onmogelijk te verslaan is  

Door gebruik te maken van een **Adversarial Self-Play AI-agent**, leert de AI door tegen zichzelf te spelen. Hierdoor:
- Ontwikkelt de AI steeds betere strategieën  
- Ontstaat een meer realistische en uitdagende tegenstander  

Dit zorgt voor een gevarieerde en boeiende spelervaring waarbij elke match anders verloopt.

---

## Waarom VR?

Virtual Reality voegt een extra laag van immersie toe die niet mogelijk is op een klassiek scherm. In deze simulatie is VR bijzonder geschikt omdat:

- De speler zich fysiek in de arena bevindt  
- Reacties gebaseerd zijn op echte bewegingen (niet enkel knoppen)  
- De speler actief moet kijken naar de tegenstander om diens acties te voorspellen  
- Het gebruik van een zwaard en schild natuurlijker aanvoelt via VR-controllers  

Hierdoor wordt de ervaring intenser en realistischer, wat belangrijk is voor een gevechtssimulatie.

---

## Interacties in de VR-omgeving

De interactie in het spel is voornamelijk fysiek en gebeurt via VR-controllers:

- De speler kan een zwaard vasthouden en gebruiken om aan te vallen  
- De speler kan een schild gebruiken om aanvallen te blokkeren  
- De speler kan vrij bewegen binnen de arena  
- De speler moet actief richten en timen om de tegenstander te raken  

De combinatie van aanval, verdediging en positionering zorgt voor een interactieve en dynamische gameplay waarin de speler constant beslissingen moet nemen.
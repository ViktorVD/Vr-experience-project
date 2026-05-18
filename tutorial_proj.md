


Wat deze tutorial leert:
    Welke versie van unity met daarbijhorende extra packages en gebruikte assets. Hoe spel verloopt.  

Installatie

    WIj hebben gewerkt met unity versie : 6000.2.7f2. 
    Waneer unity project geopend is, open :
        -   window
        -   package management
        -   package manager
        -   unity Registry
    Unity registry extra packages:
    -   ML Agents: Version 2.0.2
    -   XR Interaction Toolkit: Version 3.2.2
    -   Oculus XR Plugin: Version 4.5.4
    -   OpenXR Plugin: Version 1.15.1

    Klik op volgende links voor de assets toe te voegen aan je account.
    Asset voor schild: https://assetstore.unity.com/packages/p/low-poly-3d-wooden-shield-pack-359602
    Asset voor zwaard: https://assetstore.unity.com/packages/p/free-low-poly-swords-rpg-weapons-198166
    Asset voor arena : !!!!!!!!!Viktor

    Ga hierna naar My assets
        -   Klik op een asset bv : Low poly 3D wooden shield pack
        -   download
        -   import to project
        -   import

ML Agent

    Observaties

        De afstand van zichzelf tot tegenstaander.
        float dist = Vector3.Distance(transform.position, opponent.transform.position);
        sensor.AddObservation(dist / 10f);

        Nog nakijken
        sensor.AddObservation(transform.InverseTransformDirection((opponent.transform.position - transform.position).normalized));
        sensor.AddObservation(transform.InverseTransformPoint(opponent.physicalSword.transform.position));
        sensor.AddObservation(transform.InverseTransformPoint(opponent.physicalShield.transform.position));


        Of de tengestander aanvalt.
        sensor.AddObservation(opponent.isAttacking ? 1f : 0f);

        Of de tegenstander blokkeert
        sensor.AddObservation(opponent.isBlocking ? 1f : 0f);

        Welke richting de tegenstander blokkeert.
        sensor.AddObservation((float)opponent.currentBlockDir / 3f);
        
        Of hij zelf aanvalt.
        sensor.AddObservation(isAttacking ? 1f : 0f);

        OF hij zelf blokkeert.
        sensor.AddObservation(isBlocking ? 1f : 0f);

        Cooldown tussen acties, om te voorkomen dat hij honderden keren per seconde aanvalt of blokkeert.
        sensor.AddObservation(globalActionCooldown > 0 ? 1f : 0f);
        
        Hitpoints van agent
        sensor.AddObservation(myHealth.currentHealth / 100f);

        Hitpoints van tegenstander.
        sensor.AddObservation(opponent.myHealth.currentHealth / 100f);
    
    Acties

        Bewegen
            MoveIdx = 1
                Naar voor bewegen.
            MoveIdx = 2
                Naar achter bewegen.
            MoveIdx = 3
                Naar links bewegen.
            MoveIdx = 4
                Naar rechts bewegen.

        Aanvallen
            Attacktype1: Overhead
                Assen : Y + Z
                omhoog → neer + voorwaards
            Attacktype2 : Side Swing
                Assene: X + Z
                horizontale slag van rechts naar links.
            Attacktype3: Stab
                Assen: Z
                Eerst naar achter dan naar voor.

        Blokkeren
            BlockIdx = 1
                Assen : Y + Z
                Naar voor en omhoog in het midden.
            BlockIdx = 2
                Assen : X + Y + Z
                Naar linksvoor en omhoog.
            BlockIdx = 3
                Assen : X + Y + Z
                Naar rechtsvoor en omhoog.

        Roteren
            rotIdx = 1
                Naar links roteren.
            rotIdx = 2 
                Naar rechts roteren

    Beloningen
        
        Aanvallen
            Type aanval
                Indien dezelfde type aanval als vorige aanval gebruikt wordt. 
                    - 0.02
                Indien andere type aanval als vorige aanval gebruikt wordt.
                    + 0.01

                Voorkomt dat Agent dezelfde aanval blijft uitvoeren.
                Beloont agent van variatie van aanvallen te gebruiken

            Succesvolle aanval
                Indien de aanval werd uitgevoerd maar de tegenstander niet heeft geraakt.
                    - 0.02
                Indien de aanval werd uitgevoerd en de tegenstander heeft geraakt.
                    + 0.8
                Voorkomt dat agent aanvallen uitvoert wanneer hij weinig tot 0 kans heeft op raken.
                Beloont agent agent voor aanvallen uitvoeren die succesvol zijn
            Healthpoints
                Indien agent hp tot 0 (of kleiner) wordt verminderd.
                    - 1.0
                Indien tegenstander hp tot 0 (of kleiner) wordt verminderd.
                    + 1.0

                Voorkomt dat agent te roekeloos speelt.
                Beloont agent om zijn tegenstanders hp naar 0 te verminderen.

        Blokkeren
            Actie blokkeren
                Indien agent blokkeert
                    - 0.001
                
                Voorkomt agent willekeurig gaat blokkeren
            
        Beweging
        
            Afstand tegenstander
                Indien te dicht bij tegenstander.
                    - 0.005
                Indien te ver van tegenstander. 
                    + 0.01
                Indien correcte afstand van tegenstander
                    - 0.004
                
                Voorkomt "face hugging" : agent gaat tegenstander "plakken".
                Voorkomt weglopen
                Beloont agent van zinvolle afstand tussen spelers.

            Ontwijken
                Indien tegenstander aanvalt en agent naar achter loopt/moveIdx == 2
                    + 0.005
                
                Beloont agent voor aanvallen te ontwijken aan de hand van beweging. 
        
        Rotatie
            Richting tegenstander
                Indien agent in een hoek van 30° naar tegenstander kijkt
                    + 0.005
                
                Beloont agent door richting tegenstander te kijken. 

            
# Méthodes de génération aléatoire

Ce repo est une mis en oeuvre de 4 types de générations aléatoires :
- Placement de salle simple
- BSP (Binary Space Partitioning)
- Cellular automata
- Bruit

## Placement de salle simple

Cet algorithme utilise du simple aléatoire pour placer une salle sur une grille. Si cet emplacement n'est pas disponible, il va donc refaire un aléatoire pour la replacer. Une grille trop petite avec trop de salle peut mener à une génération infinie, car aucune place n'est disponible pour x salle. Cependant, rapide a mettre en place.


## BSP

Celui la scinde aléatoirement la grile en deux, créant deux zones, qu'il va par la suite les scinder en deux également, et continuellement jusqu'à atteindre le nombre de zones requis. Cela permet une sureté de placement des salle de manière assez bien répartit, tout en restant aléatoire sur la taille des séparation.

## Cellular automata

Basé sur le fonctionnement du jeu de la [vie](https://fr.wikipedia.org/wiki/Jeu_de_la_vie), une grille est générée avec ses cases soit à 1, soit à 0, avec un pourcentage de variant suivant le choix du créateur. Par la suite, l'algorithme parcour entièrement la grille pour appliquer des règles de "vie". Dans le cas utilisé dans ce repo, si une case à 1 possède 4 ou plus voisins comme lui, il vie, sinon il passe à 0. Un nombre de passage peu être décidé pour polir la génération, et ainsi créer des zones de terre et des zones d'eau, comme pour créer des continents ou îlots.

## Bruit (ou souvant appelé Noise de l'anglais)

Le bruit est une méthode utilisé très vastement dans le domaine de la génération aléatoire. Elle peut être utilisé par example pour générer un monde avec le point le plus bas pouvant être sous l'eau, et le point le plus haut pouvant être la montagne. Mais elle peut être utilisé dans bien plus de génération qu'on peut le penser. Dans le cas de ce repos, elle a été utilisé pour générer une grotte en utilisant deux bruits différent pour le solle et le plafond, puis en les liants.
# Execution et debuggage d'algorithmes de trading sous Lean en Python

La conception, l'exécution et le debuggage d'algorithmes en Python sous un environnement Lean local est possible mais demande un peu de configuration.
Ce document liste les étapes à suivre pour y parvenir.

Des instructions sont fournies pour faire fonctionner Python sous Lean dans le document [README.md](../Algorithm.Python/Readme.md) du répertoire Algorithm.Python.

Bien qu'utiles, ces instructions sont incomplètes et ce document se propose de les compléter.

## Installation de l'environnement virtuel Python

La plupart d'entre vous disposent d'une installation Anaconda, je vous propose d'installer un environnement virtuel de préfixe local dédié.

Pour cela, ouvrez un terminal Anaconda/Mini conda en mode administrateur et naviguez jusqu'au répertoire Lean/Launcher/bin/Debug que nous avons utilisé pour exécuter Lean jusqu'à présent.

La commande suivante créée un environnement virtuel de préfixe local "venv" dans le répertoire courant avec la version de Python indiquée dans la documentation officielle :

```bash
conda create --prefix ./venv python=3.8.13
```

La commande suivante active l'environnement virtuel nouvellement créé:

```bash
conda activate ./venv
```

La commande suivante installe les packages Python nécessaires à l'exécution de Lean dans l'environnement installé et activé:

```bash
pip install pandas==1.4.3
pip install wrapt==1.14.1
```

La commande suivante installe les packages Python nécessaires à l'obtention de l'intellisense dans vos environnements de développement

```bash
pip install --upgrade quantconnect-stubs
```


## Mise à jour des variables d'environnement.

La documentation officielle indique qu'il faut ajouter le chemin complet du fichier "Lean/Launcher/bin/Debug//venv/python38.dll" à la variable d'environnement PYTHONNET_PYDLL.

Il se trouve qu'en plus la variable PYTHONHOME devra être définie au chemin complet du répertoire "Lean/Launcher/bin/Debug/venv".

Vous avez la possibilité de définir ces variables d'environnement au niveau système (tapez "env" dans votre menu démarrer sous Windows et lancez l'application "Modifier les variables d'environnement système").

Ceci dit, cela risque d'affecter d'autres applications Python que vous pourriez avoir installées sur votre machine. 

Il est également possible de définir ces variables d'environnement dans le profil de démarrage de l'application Launcher. Pour ce faire, faites un click droit sur le projet "QuantConnect.Lean.Launcher" dans l'explorateur de solution Visual Studio et sélectionnez "Propriétés". Dans l'onglet "Déboguer", clickez sur le lien "Profils de lancement de démarrage" et ajoutez les variables d'environnement à l'emplacement indiqué.


## Mise à jour du fichier config.json et lancement de l'algorithme

Dans notre prise en main de Lean, nous avons appris à configurer le fichier config.json pour exécuter plusieurs algorithmes de trading en c#.
Le passage à un algorithme Python s'effectue également à cet endroit.

Modifiez les lignes suivantes en fonction de l'algorithme Python que vous souhaitez exécuter (ici la classe BasicTemplateAlgorithm dans le fichier du même nom) :

```json
    "algorithm-type-name": "BasicTemplateAlgorithm",
    "algorithm-language": "Python",
    "algorithm-location": "../../../Algorithm.Python/BasicTemplateAlgorithm.py",
```

Après ces mises à jour, lancez l'application Launcher comme précédemment; la fenêtre de terminal devrait vous permettre de visualiser l'exécution complète du backtest de l'algorithme choisi.

## Debuggage de l'algorithme Python

Pour débugger le code c# du noyau Lean ou d'un algorithme développé en c#, aucune modification n'est nécessaire: placez directement vos points d'arrêts et lancez le Launcher.

Le debuggage d'un algorithme Python est un peu plus complexe et nécessite quelques étapes supplémentaires.
Voilà les instructions pour permettre le debugage du code Python de votre algorithme dans Visual Studio.

Des ressources sur les possibilités du debugger Python sont par ailleurs fournies dans la [page suivante](https://learn.microsoft.com/en-us/visualstudio/python/debugging-python-in-visual-studio?view=vs-2022).

### Mise à jour du fichier config.json

Vous pouvez activer le debug python dans le fichier de configuration à l'aide des instructions suivantes:
    
```json
    "debugging": true,
    "debugging-method": "VisualStudio",
```

Cela aura pour conséquence de mettre l'application en pause avant le lancement de l'algorithme et d'attendre que le debugger Python de Visual studio soit attaché au processus.

Le problème est néanmoins le suivant: Visual Studio ne peut pas attacher son debugger Python à un processus pour lequel il a déjà attaché un debugger c#. c'est le cas en lançant le launcher depuis Visual Studio.

### Workaround Option 1

Il est néanmoins possible de lancer le launcher en ligne de commande ou depuis l'explorateur de fichier et d'attacher le debugger Python à ce processus.

Le problème est alors que l'on ne bénéficiera plus de la configuration de lancement VS que nous avons précédemment définie pour y adjoindre une variable d'environnement nécessaire à l'exécution du code Python (cf le fichier "\Launcher\Properties\launchSettings.json")

Qu'à cela ne tienne, nous pouvons également définir la variable d'environnement dans un terminal Powershell avant de lancer l'application.

Dans un explorateur de fichier, naviguez dans le répertoire "Lean/Launcher/bin/Debug" et lancez un terminal Powershell dans ce répertoire (Fichier+Ouvrir Windows Power shell ou bien  Shift + Click droit et sélectionnez "Ouvrir une fenêtre Powershell ici").

Entrez la comamnde powershell suivante avec le chemin de votre environement Python virtuel:

```powershell
$Env:PYTHONHOME = "E:\Dev\Libs\Lean\MyIA\Launcher\bin\Debug\venv"
```

Lancez ensuite le launcher à l'aide de la commande suivante:

```powershell
dotnet QuantConnect.Lean.Launcher.dll
```

L'application devrait se lancer puis se mettre en pause et attendre que le debugger Python soit attaché.

### Workaround Option 2

L'autre possibilité, plus simple, est de détacher le debugger .Net de Visual Studio, avant de réattacher le debugger en n'oubliant pas de changer le type de debugger de .Net managé à Python


### Attachement du debugger Python

Placez au préalable un point d'arrêt dans votre code Python avant d'aller plus loin, car l'attachement mettra fin à la pause et déclanchera l'exécution de l'algorithme jusqu'à votre point d'arrêt.

Dans Visual Studio, ouvrez le menu "Déboguer" et sélectionnez "Attacher au processus".

Au niveau du champs "Attacher à", sélectionnez "Python".

Dans la liste des processus, sélectionnez le processus "dotnet.exe" (Python est embarqué dans le c#) et cliquez sur le bouton "Attacher".

Vous devriez désormais pouvoir débugger votre code Python comme n'importe quel code c#.

### Utilisation d'un autre environnement de développement et de debug Python

Il est également possible de travailler en Python dans un autre IDE.

Voir ce [fichier de code](../AlgorithmFactory/DebuggerHelper.cs) pour les commandes alternatives à "Visual Studio" dans le fichier config.json

Et probablement [cette page](https://www.quantconnect.com/docs/v2/lean-cli/projects/autocomplete#07-Imports) pour l'import des symboles nécessaires à l'auto-completion


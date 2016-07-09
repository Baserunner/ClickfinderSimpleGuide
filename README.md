# Clickfinder Simple Guide
## Overview
This is a Mediaportal Plugin that shows TV-Movie Data in a simple list form. 
The data becomes originally downloaded from Clickfinder and loaded into MediaPortal Database by the TvMovieImport plugin.

This plugin is based on [Clickfinder ProgramGuide Plugin](http://forum.team-mediaportal.com/threads/clickfinder-programguide-1-6-1-0.108586/) from Scrounger. 
All credits to him for all his excellent effort.

**Requirements:**
* [Mediaportal](http://www.team-mediaportal.de)
* [TvMovieImport 1.6.4.2](http://forum.team-mediaportal.com/threads/clickfinder-programguide-1-6-1-0.108586/)
* [TV Movie ClickFinder](http://www.tvmovie.de/news/tv-movie-clickfinder-84949) (no Premium Account required)
* [ClickFinder Simple Guide Plugin itself](https://github.com/Baserunner/ClickfinderSimpleGuide/tree/master/mpe1)

## Introduction
(As this is, most probably for German users only, I switch to German now)

Das Plugin soll TV-Movie EPG-Daten auf eine m�glichst einfache und schnelle Art und Weise anzeigen. Es soll per Tastendruck zeigen, was zur "Prime-Time" im Fernsehen l�uft, welche Spielfilme heute laufen oder in der n�chsten Woche kommen, und so weiter.
F�r diesen Zweck stellt das Plugin verschiedene Anzeigen, im Folgenden Views, zur Verf�gung. Insgesamt k�nnen bis zu 8 verschiedene Views definiert und �ber die Tasten 1-8 aufgerufen werden.

Mit der Taste 0 wird das Programm f�r einen einzelnen Kanal aufgerufen.

Die Bedienung soll dem Bl�ttern in einem normalen TV-Magazin �hnlich sein. Man kann einen Tag, eine Stunde vor- und zur�ckbl�ttern, durch Kanalgruppen schauen und IMDB/TMDB Infos, Taste 9, abrufen.

Die Views 1-8 sind �ber das Setup Programm konfigurierbar. Prinzipiell k�nnen die Views, SQL-Know-How vorrausgesetzt, unter gewissen Einschr�nkungen, frei definiert werden. Es sind 8 Default-Views vordefiniert.

Urspr�nglich hatte ich das Plugin als Kategorie vom ProgramGuide Plugin geplant. Leider hatte ich Probleme mit der Stabilit�t und dann nach und nach Funktionen, die ich nicht nutze ausgebaut. Irgendwann hatte ich ein eigenst�ndiges Plugin. 
Das Plugin hat bei weitem nicht die Funktionalit�t des Clickfinder Program Guides. Wer Serien, Video, Moving-Picture Unterst�tzung erwartet wird von diesem Plugin ent�uscht sein. Deswegen auch der Name "Simple Guide".

Derzeit wird nur der Titan Skin unterst�tzt.
## Installation
[Hier](https://github.com/Baserunner/ClickfinderSimpleGuide/tree/master/mpe1) befindet sich das .mpe1 File. Der Rest d�rfte bekannt sein.

## Bedienung

**(1)**: Ruft View 1 auf (Default ist die �bersicht "Jetzt")

**(2)**: Ruft View 2 auf (Default ist die �bersicht "Prime Time" - 20:15)

**(3)**: Ruft View 3 auf (Default ist die �bersicht "Late Time" - 22:00)

**(4)**: Ruft View 4 auf (Default ist die �bersicht "Night" - 01:00)

**(5)**: Ruft View 5 auf (Default ist die �bersicht "Filme")

**(6)**: Ruft View 6 auf (Default ist die �bersicht "Filme Vorschau")

**(7)**: Ruft View 7 auf (Default ist die �bersicht "TV-Movie Rating")

**(8)**: Ruft View 8 auf (Default ist die �bersicht "Tagestipps") *(funktioniert nicht bei allen Installationen)*

**(9)**: Ruft TMDB/IMDB Informationen vom jeweiligen Programm ab

**(0)**: Ruft die Einzelkanal View auf

**(up/down)**: Navigiere +1/-1

**(page up/down)**: Navigiere +7/-7

**(right)**: Eine Kanalgruppe vorw�rts, in der Einzelkanal Sicht einen Kanal vorw�rts

**(left)**: Eine Kanalgruppe zur�ck, in der Einzelkanal Sicht einen Kanal zur�ck

**Next Item (F8)**: Einen Tag vorw�rts

**Previous Item (F7)**: Einen Tag zur�ck

**Forward (F6)**: Einen Stunde vorw�rts

**Rewind (F5)**: Einen Stunde zur�ck

**Play Button (P)**: Schalte auf den Kanal

**Record Button (R)**: Aufnehmen

**Menu Button (F9)**: Hilfe

**OSD Info Button (Y)**: Hilfe

## Screenshots
### Plugin
![Jetzt View](https://cloud.githubusercontent.com/assets/8837272/16694064/8d8e5164-4538-11e6-9552-42c543ee9e24.png)
![TagesTipps View](https://cloud.githubusercontent.com/assets/8837272/16693731/db7e7e00-4536-11e6-8173-fc147322f2cc.png)
![Single Channel View](https://cloud.githubusercontent.com/assets/8837272/16706449/228d3428-45ad-11e6-8d94-12c9056d13d3.png)

### Configuration
<div align="center">
        <img width="45%" src="https://cloud.githubusercontent.com/assets/8837272/16694176/1768ffce-4539-11e6-90fe-faf02ab84ce5.png" alt="Config general" title="Config general"</img>
        <img height="0" width="8px">
        <img width="45%" src="https://cloud.githubusercontent.com/assets/8837272/16694201/36f4299a-4539-11e6-8166-a7c0c2fd2627.png" alt="Config View 5" title="Config View 5"></img>
</div>
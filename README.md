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

Das Plugin soll TV-Movie EPG-Daten auf eine m�glichst einfache und schnelle Art und Weise anzeigen. Eine Anforderung war es, dass man einfach per Tastendruck sieht, was zur "Prime-Time" im Fernsehen l�uft, welche Spielfilme in der n�chsten Woche kommen, etc. F�r diesen Zweck stellt das Plugin verschiedene Views zur Verf�gung. Insgesamt kann man sich bis zu 8 verschiedene, konfigurierbare Views definieren und �ber die Tasten 1-8 aufrufen.

Mit der Taste 0 wird das Programm f�r einen einzelnen Kanal aufgerufen

Die Bedienung soll dem Lesen eines normalen TV-Magazin nach empfunden sein. Man kann einen Tag, eine Stunde vor- und zur�ckbl�ttern, durch Kanalgruppen schauen und IMDB/TMDB Infos, Taste 9, abholen.

Die Views 1-8 sind �ber das Setup Programm konfigurierbar. Prinzipiell k�nnen die Views, SQL-Know-How vorrausgesetzt, unter gewissen Einschr�nkungen, frei definiert werden. Nat�rlich habe ich 8 Default-Views vordefiniert.

Derzeit wird nur der Titan Skin unterst�tzt.
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
<div align="center">
        <img width="45%" src="https://cloud.githubusercontent.com/assets/8837272/16694064/8d8e5164-4538-11e6-9552-42c543ee9e24.png" alt="Jetzt screen" title="Jetzt Screen"</img>
        <img height="0" width="8px">
        <img width="45%" src="https://cloud.githubusercontent.com/assets/8837272/16693731/db7e7e00-4536-11e6-8173-fc147322f2cc.png" alt="Tagestipp screen" title="Tagestipp Screen"></img>
</div>

### Configuration
<div align="center">
        <img width="45%" src="https://cloud.githubusercontent.com/assets/8837272/16694176/1768ffce-4539-11e6-90fe-faf02ab84ce5.png" alt="Config general" title="Config general"</img>
        <img height="0" width="8px">
        <img width="45%" src="https://cloud.githubusercontent.com/assets/8837272/16694201/36f4299a-4539-11e6-8166-a7c0c2fd2627.png" alt="Config View 5" title="Config View 5"></img>
</div>
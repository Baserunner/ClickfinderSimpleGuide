# Clickfinder Simple Guide
## Overview
This is a Mediaportal Plugin that shows TV-Movie Data in a simple list form, that is usually shown by Clickfinder. This is for German TV data only.

This plugin is based on [Clickfinder ProgramGuide Plugin](http://forum.team-mediaportal.com/threads/clickfinder-programguide-1-6-1-0.108586/) from Scrounger. All credits to him for all his excellent effort 

**Requirements:**
* [Mediaportal](http://www.team-mediaportal.de)
* [TvMovieImport 1.6.4.2](http://forum.team-mediaportal.com/threads/clickfinder-programguide-1-6-1-0.108586/)
* [TV Movie ClickFinder](http://www.tvmovie.de/news/tv-movie-clickfinder-84949) (no Premium Account required)
* [ClickFinder Simple Guide Plugin itself](https://github.com/Baserunner/ClickfinderSimpleGuide/tree/master/mpe1)

## Introduction
(As this is, most probably for German users only I switch to German now)

Das Plugin soll TV-Movie Programm-Daten auf eine möglichst einfache und schnelle Art und Weise anzeigen. Beispielsweise soll es möglich sein einfach per Tastendruck zu sehen von "Jetzt" im Fernsehen läuft, zur "Prime-Time", welche Spielfilme, etc. ... Es sind insgesamt 8 verschiedene, frei konfigurierbare Views, möglich (Tasten 1-8). Ich habe diese Funktionalität seit dem ich 3PG (EPG Plugin für den Topfield) nicht mehr nutzen kann vermisst.

Mit der Taste 0 wird das Programm für einen einzelnen Kanal aufgerufen

Die Bedienung soll dem Lesen eines normalen TV-Magazin nachempfunden sein. Man kann einen Tag, eine Stunde vor- und zurückblättern, durch Kanalgruppen schauen und IMDB/TMDB Infos abholen.

Derzeit wird nur der Titan Skin unterstützt.

Die Views 1-8 sind über das Setup Programm konfigurierbar.
## Bedienung

**(1)**: Ruft View 1 auf (Default ist die Übersicht "Jetzt")

**(2)**: Ruft View 2 auf (Default ist die Übersicht "Prime Time" - 20:15)

**(3)**: Ruft View 3 auf (Default ist die Übersicht "Late Time" - 22:00)

**(4)**: Ruft View 4 auf (Default ist die Übersicht "Night")

**(5)**: Ruft View 5 auf (Default ist die Übersicht "Filme")

**(6)**: Ruft View 6 auf (Default ist die Übersicht "Filme Vorschau")

**(7)**: Ruft View 7 auf (Default ist die Übersicht "TV-Movie Rating")

**(8)**: Ruft View 8 auf (Default ist die Übersicht "Tagestipps") *(funktioniert nicht bei allen Installationen)*

**(9)**: Ruft TMDB/IMDB Informationen vom jeweiligen Programm ab

**(0)**: Ruft die Einzelkanal View auf

**(up/down)**: Navigiere +1/-1

**(page up/down)**: Navigiere +7/-7

**(right)**: Eine Kanalgruppe vorwärts, in der Einzelkanal Sicht einen Kanal vorwärts

**(left)**: Eine Kanalgruppe zurück, in der Einzelkanal Sicht einen Kanal zurück

**Next Item (F8)**: Einen Tag vorwärts

**Previous Item (F7)**: Einen Tag zurück

**Forward (F6)**: Einen Stunde vorwärts

**Rewind (F5)**: Einen Stunde zurück

**Play Button (P)**: Schalte auf den Kanal

**Record Button (R)**: Aufnehmen

**Menu Button (F9)**: Hilfe

**OSD Info Button (Y)**: Hilfe

## Screenshots
### Plugin
![Filme](/Screenshots/Screenshot_Filme.png?raw=true)
![Tagestipps](/Screenshots/Screenshot_Tagestipps.png)
![SingleChannel](/Screenshots/Screenshot_SingleChannel.png)
![Jetzt](/Screenshots/Screenshot_Jetzt.png)
### Configuration
![Übersicht](/Screenshots/Screenshot_Config1.png)             
![Allgemein](/Screenshots/Screenshot_Config2.png)
![View5](/Screenshot_Config3.png)
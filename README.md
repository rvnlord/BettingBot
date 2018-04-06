# BettingBot - Overview of Features Implemented so Far 
(Work in progress, sorry no english Localization yet)

* Results are presented in Customizable Table

![Results Table](/Images/2018-04-06_165741.png?raw=true "Results Table")

* There is wide variety of options you can apply to your simulated strategy depending on chosen tipster tips

![Calculations](/Images/2018-04-06_165824.png?raw=true "Calculations")

* You can review your strategy statistics

![Statistics](/Images/2018-04-06_165904.png?raw=true "Statistics")

* You can load tipsters and tips from supported sites to database by providing their URL (currently supported are HintWise and BetShoot, program is using Selenium because sites do not provide any kind of API). If site requires credentials, you can specify them too on the bottom.

![Database](/Images/2018-04-06_170047.png?raw=true "Database")

* You can change various settings in the panel

![Options](/Images/2018-04-06_170026.png?raw=true "Options")

* There is also another utility flyout that allows you to calculate double chance and surebet manually in case you need it

![Calculator](/Images/2018-04-06_170701.png?raw=true "Calculator")

* Program is portable (using SQLite accessed via EF)
* [NEW] Now Selenium headless mode with visual feedback

![SeleniumHeadlessFeedback](/Images/2018-04-06_165157.png?raw=true "SeleniumHeadlessFeedback")

* The ultimate goal of this software is to automate betting process entirely so it can be more easily considered as an investment option. For now however you can use it for advanced analysis of tipster performance.
* Be aware that Win/Lose Aggregated Statistics are Lose Condition Dependent.








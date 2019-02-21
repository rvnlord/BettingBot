## BettingBot v0.28a-alpha

### Download:

[![Download](https://img.shields.io/badge/download-BettingBot--v0.28--alpha-blue.svg)](https://github.com/rvnlord/BettingBot/releases/download/v0.28-alpha/BettingBot-v0.28-alpha.zip)
![Calculations](https://img.shields.io/badge/SHA--256-8EB61D86A6CBD841862E676B4A41DC5176E1C6931107170681B6F5E99575F04E-green.svg)

### How to use:

1. Change language to English if you can't understand a thing.

   ![Change language](/Images/2018-09-06_145434.png?raw=true)

2. Add tipsters, add login to broker's website (if you plan to test placing bets) and download tipsters' advices.

   ![Load data](/Images/2018-09-06_145956.png?raw=true)

3. Make sure to include your API key for external website, otherwise your matches won't be linked to additional source.

   ![Provide API Key](/Images/2018-09-06_150352.png?raw=true)

4. Create and calculate your strategy

   ![Provide API Key](/Images/2018-09-06_150657.png?raw=true)

5. Make bet, it should be visible in "Bets" tab.

   ![Provide API Key](/Images/2018-09-06_150723.png?raw=true)

### Overview of features implemented so Far

* Betting from GUI according to selected strategy based on tipster's advices from Betshoot and Hintwise websites (though the latter banned its users recently and replaced them by "best" performing random algorithms, which is basically a scam)
* Statistics and performance analysis with visual feedback
* Calculator of Surebets, Double chances and future stakes
* Currently most of the app has been localized so GUI is available in both English and Polish languages

### Tools

Application loads data using three different aproaches, the one used with particular website is determined based on its efficiency when retrieving data. Methods are:
* API HTTP Request + JSON Parsing
* Document HTTP Request + HtmlAgility Parsing
* Selenium Webdriver + Selenium Parsing

Though keep in mind that some of them can't be used in some cases. That's why often it is necessary to use slower ones.

Application fully supports Selenium headless mode with Chrome, should you ever need to change this option, it can be achieved from Settings Panel.

### Goals, TODOs and Known Bugs

The ultimate goal of this software is to automate betting process entirely, so it can be more easily considered as an investment option. For now however you can use it for advanced analysis of tipster performance and betting on supported broker platform.

TODOs:
* Finish English localization (loaders, prompts, errors and unparsed bets are still not localized)
* Use API for converting currencies as I was forced recently to hardcode it to using EUR.
* Write viable replacement for Football-Data API. In this app arbitrary and complete source of information is needed but wrapping 3rd party website using Selenium is time consuming. Ideally I would like to use FlashScore or WhoScored.
* AO often blocks login process or redirects randomly, this is hard to predict and even harder to solve. The website is full of surprising bugs. I implemented numerous walkarounds but you might still be sometimes prompted about inability to login.
* Hintwise probably won't work properly anymore because they removed all their tipsters.
* Sometimes unexpected discipline might cause data loading errors.
* Sometimes discipline might be unlinked from league.

### Afterword

To be honest this app is hard to maintain as it requires a lot of time to constantly improve parsing mechanisms. Even minor change to external website has proven to be able to break functionality of the program (Like when Betshoot layout changed slightly in may 2018). I created this project for my Master's degree which I achieved on 12th June 2018. Since that day I still continued to work on this project (mainly because it surprisingly gained unexpected attention). However I am struggling to find time to continue developing this project. Therefore, work on the application is suspended until further notice. 

My main interest is blockchain, machine learning algorithms for making various predictions and automation of trading and betting experience.

I am currently working remotely as a freelancer.

I would like to thank all of you for your interest in my project and for suggestions how to make it better. It is an enriching experience for me. Especially because I am still fairly new and inexperienced programmer.

Recently someone asked me in an email whether I would continue working on this project if I received enough donations so I could pay my bills with it. I replied that it is a good idea but if you statistically look at donations then you can verify that it isn't really possible. Though to be honest, if I made 320 EUR from donations alone per month I could just dump the freelancing for now and start producing viable commits. It would be exciting because I have always wanted to reinforce betting experience with machine learning, add charts, replace football-data API with something better and fix some really riddiculus bugs.

BTC: 1Gb4iwq18FQDpGug1hxyWC8URBDaQ6qLYr
ETH: 0x6AA24e9B826CA0c50458C7d95d75A061A4908ee6
XRP: rMsefHwYUVj3TqgNgSSZVdD5BLtr8amKLa
NEO: AKHf57YdXze4ew8UV12Q8Tggzf6k8FPKS8
XMR: 43pTPPf3Eo27yCmf8zfLF7fP5NeGcfMxQJQHGwTRN17jRmETUa7UP4kBUtosUDLLPgU6RoRLrsma5GtyY8P4KobG98JHXu8









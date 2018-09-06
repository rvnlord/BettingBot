## BettingBot v0.28-alpha

### Download:

[![Download](https://img.shields.io/badge/download-BettingBot--v0.28--alpha-blue.svg)](https://github.com/rvnlord/BettingBot/releases/download/v0.28-alpha/BettingBot-v0.28-alpha.zip)
![Calculations](https://img.shields.io/badge/SHA--256-8EB61D86A6CBD841862E676B4A41DC5176E1C6931107170681B6F5E99575F04E-green.svg)

### How to use:

1. Change language to english if you can't understand a thing.

   ![Change language](/Images/2018-09-06_145434.png?raw=true)

2. Add tipsters, add login to broker's website if you plan to test betting and download bets data.

   ![Load data](/Images/2018-09-06_145956.png?raw=true)

3. Make sure to include your API key for external website, otherwise your matches won't be linked to additional source.

   ![Provide API Key](/Images/2018-09-06_150352.png?raw=true)

4. Create and calculate your strategy

   ![Provide API Key](/Images/2018-09-06_150657.png?raw=true)

5. Make bet, it should be visible in "Bets" tab.

   ![Provide API Key](/Images/2018-09-06_150723.png?raw=true)

### Overview of features implemented so Far:

* Betting from GUI according to selected strategy based on tipster's advices from Betshoot and Hintwise websites (though the latter banned its users recently and replaced them by "best" performing random algorithms, which basically is a scam)
* Statistics and performance analysis with visual feedback
* Calculator of Surebets, Double chances and future stakes
* Currently most of the app has been localized so GUI is available in English and Polish

### Tools

Application basically loads data using three different methods, the one used with particular website is selected based on its speed whewn retrieving data. Methods are:
* API HTTP Request + JSON Parsing
* Document HTTP request + HtmlAgility Parsing
* Selenium Webdriver + Selenium Parsing
Though keep in mind that some of them can't be used in some cases. That's why often it is necessary to use slower ones.

Application fully supports Selenium headless mode with chrome, should you ever need to change this option, it can be achieved from Settings Panel.

### Goals, TODOs and Known Bugs

The ultimate goal of this software is to automate betting process entirely so it can be more easily considered as an investment option. For now however you can use it for advanced analysis of tipster performance and betting on supported broker platform.

TODOs:
* Finish English localization (loaders, prompts, errors and unparsed bets are still not localized)
* Use API for converting currencies as I was forced recently to hardcode it to using EUR.
* Code viable replacement to Football-Data API. For this app arbitrary and complete source of information is needed but wrapping 3rd party website using Selenium is time consuming. Ideally I would like to use FlashScore or WhoScored.
* AO often blocks login process or redirects randomly, this is hard to predict and even harder to solve. The website is full of surprising bugs. I implemented numerous walkarounds but still you might sometimes be prompted about inability to login.
* Hintiwse probably won't work properly anymore because they removed all it's tipsters.
* Sometimes unexpected discipline might cause data loading errors.
* Sometimes discipline might be unlinked from league.

### Afterword

To be perfectly honest, this app is hard to maintain, because it requires a lot of time from developer to constantly improve parsing mechanisms. Even minor change to external website has proven to be able to break functionality of the program (Like when Betshoot layout changed slightly in may 2018). I created this project for my Master's degree which I achieved on 12th June 2018. Since that day I didn't even have time to make decisions regarding my future and yet I still continued to work on this project (it surprisingly gained unexpected attention). Since 2010 I didn't have vacations or time for myself (for many personal reasons) and I doubt I will in forseable future. Though, now I really need some time to decide and find decent, stable job. Therefore, work on the application will be suspended until further notice. 











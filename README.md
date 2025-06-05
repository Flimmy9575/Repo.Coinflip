# Repo CoinFlip Mod
A simple mod that adds a command that allows you to gamble your money via coin-flip.


### Usage
The syntax is `/coinflip amount heads/tails` an example is `/coinflip 5000 tails`. Below are a few more examples.

```
/cf 5k t
/coinflip 5000 heads
/cf 5k tails
/coinflip 5m h
```


### Configuration
There are a few different options that can be changed in the config file.

Here is the config file: 
```toml
## Settings file was created by plugin Coinflip v1.1.0
## Plugin GUID: NotDrunkJustHigh.Coinflip

[Bet Limitations]

## The maximum amount of money that can be bet. 1 is equal 1,000(100 would set a max of 100,000) 
# Setting type: Int32
# Default value: 1000
Max Bet Amount = 1000

## The minimum amount of money that can be bet. 1 is equal 1,000
# Setting type: Int32
# Default value: 1
Min Bet Amount = 1

[General]

## Whether you can flip coins only within the shop
# Setting type: Boolean
# Default value: true
Shop Only = true

[Taxes]

## Whether or not tax is enabled
# Setting type: Boolean
# Default value: false
Tax Enabled = false

## The amount of money that is taxed on every win
# Setting type: Single
# Default value: 0.1
Tax Amount = 0.1
```




### Roadmap
- [ ] **[1.2.0]** Flip Upgrades
  - This includes configuration options
  - [ ] The ability to charge an amount of money for the upgrade (This will scale similar to the shop)
  - [ ] The ability to change how many are loss and gained (default is 2 removed on a loss and 1 gained on a win)
- [ ] **[1.3.0]** A scoreboard in the shop for flips
  - A list of all the users and how many flips they've won/loss and how much money they've earned/lost
  - [Optional] A coin hat for who has won the most flips and money
- [ ] **[1.4.0]** Host perms & Code rework
  - This upgrade would simply allow the host to have a lot more control over the mod
    - Host only
    - Limit how much money can be flipped
    - Limit how many flips can be done per shop session
    - Limit how much money can be earned and lost
  - Code rework would just be a deeper refactoring of the code

---
#### Contributing, Suggestions, Feedback, etc.
If you have any suggestions, questions, feedback or want to contribute, feel free to message me on discord(NotDrunkJustHigh).

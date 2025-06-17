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
#### Super Flips
Super flips allow you to flip multiple coins at once for a chance to win more money. To do a super flip you can execute the following command `/cf amount choice(heads/tails) superflipcount` ex. `/cf 1k h 10` If you won this even though it's like a 1% chance you could win 25k. Losing will result in you still only losing the initial bet, in this case `1k`.

---
### Configuration
There are a few different options that can be changed in the config file.

Here is the config file: 
```toml
## Settings file was created by plugin Coinflip v2.0.0
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

[Super Flips]

## Whether or not super flips are enabled
# Setting type: Boolean
# Default value: true
Super Flips Enabled = true

## The maximum amount of super flips that can be played
# Setting type: Int32
# Default value: 5
Super Flips Max = 15

## The multiplier per flip that is applied to the amount of money won on a super flip
# Setting type: Single
# Default value: 2.75
Super Flips Multiplier = 2.75

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
---



### Roadmap
- [ ] **[2.0.0]** Super Flips
  - Adds Super Flips
  - Sync Config
  - Detailed Errors(I should've did it before but, I'm lazy)
- [ ] **[2.1.0]** Flip Upgrades
  - This includes configuration options
  - [ ] The ability to charge an amount of money for the upgrade (This will scale similar to the shop)
  - [ ] The ability to change how many are loss and gained (default is 2 removed on a loss and 1 gained on a win)
- [ ] **[2.2.0]** A scoreboard in the shop for flips
  - A list of all the users and how many flips they've won/loss and how much money they've earned/lost
  - [Optional] A coin hat for who has won the most flips and money
- [ ] **[2.3.0]** Host perms & Code rework
  - This upgrade would simply allow the host to have a lot more control over the mod
    - Host only
    - Limit how much money can be flipped
    - Limit how many flips can be done per shop session
    - Limit how much money can be earned and lost
  - Code rework would just be a deeper refactoring of the code

---
#### Contributing, Suggestions, Feedback, etc.
If you have any suggestions, questions, feedback or want to contribute, feel free to message me on discord(NotDrunkJustHigh).

library(reshape2)
library(ggplot2)

data20 <- read.csv("20_Individual_152209_06042017.csv")
data50 <- read.csv("50_Individual_151445_06042017.csv")
data100 <- read.csv("100_Individual_150952_06042017.csv")

dataRunning <- data.frame(Frequency = c(data20$running,data100$running), N = (c(rep("20", nrow(data20)), rep("100", nrow(data100)))))
ggplot(dataRunning, aes(x = Frequency, fill = N)) + geom_density(alpha=.3)

dataWalking <- data.frame(Frequency = c(data20$walking,data100$walking), N = (c(rep("20", nrow(data20)), rep("100", nrow(data100)))))
ggplot(dataWalking, aes(x = Frequency, fill = N)) + geom_density(alpha=.3)

dataIdle <- data.frame(Frequency = c(data20$idle,data100$idle), N = (c(rep("20", nrow(data20)), rep("100", nrow(data100)))))
ggplot(dataIdle, aes(x = Frequency, fill = N)) + geom_density(alpha=.3)
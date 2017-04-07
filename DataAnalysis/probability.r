library(ggplot2)

alpha <- 15
tau_iw <- 35
tau_wi <- 8
tau_iwr <- 100
tau_ri <- 100
d_r <- 31.6
d_S <- 6

p_iw <- function(n_walking){(1 + alpha * n_walking) / tau_iw}
p_iw <- function(n_walking){1 - exp(-(1 + alpha * n_walking) / tau_iw)}

ggplot(data.frame(x = c(0, 10)), aes(x)) +
  stat_function(fun = p_iw) +
  ylim(0, 5)

l_i <- 50
delta <- 4
d_R <- 31.6
d_s <- 6.3

p_iwr <- function(m_running){1 - exp(-((1 / tau_iwr) * (((l_i / d_R) * (1 + alpha * m_running)) ^ delta)))}
ggplot(data.frame(x = c(0, 5)), aes(x)) +
  stat_function(fun = p_iwr) +
  ylim(0, 1)

p_ri <- function(m_idle){1 - exp(-((1 / tau_ri) * (((d_S / l_i) * (1 + alpha * m_idle)) ^ delta)))}
ggplot(data.frame(x = c(0, 5)), aes(x)) +
  stat_function(fun = p_ri) +
  ylim(0, 1)


p_iwr2 <- function(m_running){1 - exp(-(1 + alpha * m_running) * l_i)}
ggplot(data.frame(x = c(0, 5)), aes(x)) +
  stat_function(fun = p_iwr2) +
  ylim(0, 1)

alpha2 <- 150
tau_iw2 <- 35

p_iw2 <- function(p_walking){(1 + alpha2 * p_walking) / tau_iw2}
p_iw2 <- function(p_walking){1 - exp(-(1 + alpha2 * p_walking) / tau_iw2)}

ggplot(data.frame(x = c(0, 1)), aes(x)) +
  stat_function(fun = p_iw2) +
  ylim(0, 5)

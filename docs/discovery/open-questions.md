# Open Discovery Questions

These decisions should be answered before implementation of the corresponding feature. Do not silently invent business rules.

## Trading simulation

1. What virtual starting capital should V1 use?
2. What position-sizing method should be the default: fixed amount, percentage of equity, volatility-based, or risk-per-trade?
3. How many concurrent positions are allowed?
4. Are short positions allowed in paper trading?
5. Is trading intraday only, or can positions remain overnight?
6. What stop-loss methodology should be used?
7. What target methodology should be used?
8. How should slippage be simulated?
9. What transaction/fee/tax assumptions should be used?
10. What market calendar and trading-hours source should be authoritative?

## Prediction

11. What exact target should the first model predict: next-day return, N-day return, probability of positive return, or a combination?
12. Which prediction horizons should V1 expose?
13. What should constitute a sufficiently confident prediction?
14. What baseline model should be used for comparison?

## Recommendation

15. How should technical, fundamental, news and ML signals be weighted?
16. Should BUY/HOLD/SELL be rule-derived, model-derived, or a hybrid?
17. What conditions produce NO_DECISION?

## Risk

18. What are the exact maximum position, portfolio and sector exposure limits?
19. What are the maximum drawdown and daily-loss limits?
20. What minimum confidence is required before paper trading?
21. Which volatility conditions should block new trades?
22. Should risk rules differ by stock, sector or market regime?

## Monitoring

23. What monitoring cadence is required during market hours?
24. Which events require immediate alerts?
25. Which alert channels are required in V1?
26. Should the system automatically close paper positions when hard risk rules trigger?

## Data

27. Which provider supplies fundamentals in V1?
28. Which provider supplies news in V1?
29. What freshness thresholds apply to market, fundamental and news data?
30. How should provider conflicts be resolved when multiple providers are introduced?

## AI

31. Which cloud AI model should be the initial default?
32. Which local Ollama model should be the initial supported/tested model?
33. Should AI research be allowed to use external web research directly, or only normalized platform data plus an approved research tool?
34. What structured output validation/retry policy should be used for AI responses?

## Evaluation

35. What historical evaluation period is required?
36. What benchmark should recommendations be compared against?
37. What minimum sample size is required before calling a strategy/model useful?

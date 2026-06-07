# Investee Chatbot Intelligence

## Hybrid Architecture
The Investee Chatbot uses a **Priority Hybrid Logic** to ensure speed and accuracy:

1. **Local NLP (Fast Match):** Queries regarding coin prices, 24h changes, and user portfolio P&L are intercepted before reaching the AI.
2. **AI Shuffler (Advanced Context):** General questions and deep market analysis are handled by a multi-model shuffler (Gemini 2.5 Flash / 2.0 Flash).
3. **Emergency Failsafe:** If the AI API is unreachable, the system automatically fulfills queries using local database snapshots (Emergency Mode).

## Data Capabilities
- **Market Data:** Live prices and 24h % changes for major coins (BTC, ETH, SOL, etc.).
- **Portfolio Tracking:** Real-time calculation of Unrealized P&L and asset distribution.
- **Fail-Forward:** The bot never apologizes for "not having data" if it exists in the system.

## Performance
- **Caching:** Price data is cached for 30 seconds to prevent API rate limiting (429).
- **Latency:** Local responses are < 100ms. AI responses have a 10s timeout before shuffling.

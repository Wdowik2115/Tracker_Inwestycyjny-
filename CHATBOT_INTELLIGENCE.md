# Investee Chatbot Intelligence

## Hybrid Architecture
The Investee Chatbot uses a **Priority Hybrid Logic** to ensure speed and accuracy:

1. **Local NLP (Fast Match):** Queries regarding coin prices, portfolio P&L, recent transactions, and active alerts are intercepted locally.
2. **AI Shuffler (GeminiApiService):** General questions and deep market analysis are handled by a multi-model shuffler (Gemini 2.5 Flash / 2.0 Flash) with advanced context injection.
3. **Resilience & Failsafe:** 
   - **Polly Integration:** Implementation of Retry (exponential backoff) and Circuit Breaker for API calls.
   - **Emergency Mode:** If the AI API is unreachable after retries, the system falls back to a locally-generated response based on available database snapshots.

## Data Capabilities
- **Market Data:** Live prices and 24h % changes.
- **Portfolio Tracking:** Real-time calculation of Unrealized P&L, asset distribution, and transaction history.
- **Alert Awareness:** Knowledge of user's active price alerts and their targets.
- **Fail-Forward:** The bot never apologizes for "not having data" if it exists in the local system.

## Performance & UX
- **Caching:** Price data is cached (30s) to prevent API rate limiting.
- **Latency:** Local responses < 100ms. 
- **User Feedback:** Visual "Slow Response" indicator in UI for AI analyses taking > 8s.

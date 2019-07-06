using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderMatcher
{
    public class MatchingEngineHost
    {
        private readonly Dictionary<string, (MatchingEngine, CancellationTokenSource)> _matchingEngines;
        public MatchingEngineHost()
        {
            _matchingEngines = new Dictionary<string, (MatchingEngine, CancellationTokenSource)>();
        }

        public void Start(string market)
        {
            var eventListener = new EventListener(null, null);
            if (!_matchingEngines.ContainsKey(market))
            {
                var matchingEngine = new MatchingEngine(eventListener, new TimeProvider());
                var cancellationTokenSource = new CancellationTokenSource();
                _matchingEngines.Add(market, (matchingEngine, cancellationTokenSource));
                matchingEngine.Run(cancellationTokenSource.Token);
            }
        }
    }
}

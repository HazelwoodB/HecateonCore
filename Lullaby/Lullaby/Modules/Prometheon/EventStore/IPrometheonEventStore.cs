using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hecateon.Modules.Prometheon.Models;

namespace Hecateon.Modules.Prometheon.EventStore
{
    /// <summary>
    /// Append-only event store for Prometheon cognitive events.
    /// Follows HecateonCore principles: immutable, versioned, auditable.
    /// </summary>
    public interface IPrometheonEventStore
    {
        /// <summary>
        /// Append an operator state snapshot.
        /// </summary>
        Task<OperatorState> AppendStateAsync(OperatorState state);

        /// <summary>
        /// Append a transaction event.
        /// </summary>
        Task<Transaction> AppendTransactionAsync(Transaction transaction);

        /// <summary>
        /// Append a narrative reframe event.
        /// </summary>
        Task<NarrativeFrame> AppendReframeAsync(NarrativeFrame reframe);

        /// <summary>
        /// Append a foresight simulation event.
        /// </summary>
        Task<ForesightSimulation> AppendSimulationAsync(ForesightSimulation simulation);

        /// <summary>
        /// Get latest operator state.
        /// </summary>
        Task<OperatorState?> GetLatestStateAsync(string? userId = null);

        /// <summary>
        /// Get operator state history.
        /// </summary>
        Task<IEnumerable<OperatorState>> GetStateHistoryAsync(
            DateTime? since = null,
            int? limit = null,
            string? userId = null);

        /// <summary>
        /// Get transaction history.
        /// </summary>
        Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(
            DateTime? since = null,
            int? limit = null,
            string? userId = null);

        /// <summary>
        /// Get reframe history.
        /// </summary>
        Task<IEnumerable<NarrativeFrame>> GetReframeHistoryAsync(
            DateTime? since = null,
            int? limit = null,
            string? userId = null);

        /// <summary>
        /// Get simulation history.
        /// </summary>
        Task<IEnumerable<ForesightSimulation>> GetSimulationHistoryAsync(
            DateTime? since = null,
            int? limit = null,
            string? userId = null);

        /// <summary>
        /// Get specific state by ID.
        /// </summary>
        Task<OperatorState?> GetStateByIdAsync(Guid id);

        /// <summary>
        /// Get specific transaction by ID.
        /// </summary>
        Task<Transaction?> GetTransactionByIdAsync(Guid id);

        /// <summary>
        /// Get specific reframe by ID.
        /// </summary>
        Task<NarrativeFrame?> GetReframeByIdAsync(Guid id);

        /// <summary>
        /// Get specific simulation by ID.
        /// </summary>
        Task<ForesightSimulation?> GetSimulationByIdAsync(Guid id);
    }

    /// <summary>
    /// In-memory implementation of Prometheon event store.
    /// For production, replace with encrypted SQLite implementation.
    /// </summary>
    public class InMemoryPrometheonEventStore : IPrometheonEventStore
    {
        private readonly List<OperatorState> _states = new();
        private readonly List<Transaction> _transactions = new();
        private readonly List<NarrativeFrame> _reframes = new();
        private readonly List<ForesightSimulation> _simulations = new();
        private readonly object _lock = new();

        public Task<OperatorState> AppendStateAsync(OperatorState state)
        {
            lock (_lock)
            {
                _states.Add(state);
                return Task.FromResult(state);
            }
        }

        public Task<Transaction> AppendTransactionAsync(Transaction transaction)
        {
            lock (_lock)
            {
                _transactions.Add(transaction);
                return Task.FromResult(transaction);
            }
        }

        public Task<NarrativeFrame> AppendReframeAsync(NarrativeFrame reframe)
        {
            lock (_lock)
            {
                _reframes.Add(reframe);
                return Task.FromResult(reframe);
            }
        }

        public Task<ForesightSimulation> AppendSimulationAsync(ForesightSimulation simulation)
        {
            lock (_lock)
            {
                _simulations.Add(simulation);
                return Task.FromResult(simulation);
            }
        }

        public Task<OperatorState?> GetLatestStateAsync(string? userId = null)
        {
            lock (_lock)
            {
                var state = _states
                    .Where(s => userId == null || s.AlgorithmVersion == userId) // Placeholder for userId
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefault();
                return Task.FromResult(state);
            }
        }

        public Task<IEnumerable<OperatorState>> GetStateHistoryAsync(
            DateTime? since = null,
            int? limit = null,
            string? userId = null)
        {
            lock (_lock)
            {
                var query = _states.AsEnumerable();
                
                if (since.HasValue)
                    query = query.Where(s => s.Timestamp >= since.Value);

                query = query.OrderByDescending(s => s.Timestamp);

                if (limit.HasValue)
                    query = query.Take(limit.Value);

                return Task.FromResult(query);
            }
        }

        public Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(
            DateTime? since = null,
            int? limit = null,
            string? userId = null)
        {
            lock (_lock)
            {
                var query = _transactions.AsEnumerable();
                
                if (since.HasValue)
                    query = query.Where(t => t.Timestamp >= since.Value);

                query = query.OrderByDescending(t => t.Timestamp);

                if (limit.HasValue)
                    query = query.Take(limit.Value);

                return Task.FromResult(query);
            }
        }

        public Task<IEnumerable<NarrativeFrame>> GetReframeHistoryAsync(
            DateTime? since = null,
            int? limit = null,
            string? userId = null)
        {
            lock (_lock)
            {
                var query = _reframes.AsEnumerable();
                
                if (since.HasValue)
                    query = query.Where(r => r.Timestamp >= since.Value);

                query = query.OrderByDescending(r => r.Timestamp);

                if (limit.HasValue)
                    query = query.Take(limit.Value);

                return Task.FromResult(query);
            }
        }

        public Task<IEnumerable<ForesightSimulation>> GetSimulationHistoryAsync(
            DateTime? since = null,
            int? limit = null,
            string? userId = null)
        {
            lock (_lock)
            {
                var query = _simulations.AsEnumerable();
                
                if (since.HasValue)
                    query = query.Where(s => s.Timestamp >= since.Value);

                query = query.OrderByDescending(s => s.Timestamp);

                if (limit.HasValue)
                    query = query.Take(limit.Value);

                return Task.FromResult(query);
            }
        }

        public Task<OperatorState?> GetStateByIdAsync(Guid id)
        {
            lock (_lock)
            {
                return Task.FromResult(_states.FirstOrDefault(s => s.Id == id));
            }
        }

        public Task<Transaction?> GetTransactionByIdAsync(Guid id)
        {
            lock (_lock)
            {
                return Task.FromResult(_transactions.FirstOrDefault(t => t.Id == id));
            }
        }

        public Task<NarrativeFrame?> GetReframeByIdAsync(Guid id)
        {
            lock (_lock)
            {
                return Task.FromResult(_reframes.FirstOrDefault(r => r.Id == id));
            }
        }

        public Task<ForesightSimulation?> GetSimulationByIdAsync(Guid id)
        {
            lock (_lock)
            {
                return Task.FromResult(_simulations.FirstOrDefault(s => s.Id == id));
            }
        }
    }
}

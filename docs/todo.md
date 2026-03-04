# TODO

- [ ] Investigate Wolverine internal `dbcontrol://` traffic visibility.
  - Observed repeated `DatabaseOperationBatch` debug logs approximately every second in WebApi.
  - Note: this is expected Wolverine internal durable messaging coordination (inbox/outbox/control queue), not business handler traffic.
  - Action: reduce Wolverine message execution log verbosity in production if noisy.
  - Action: verify actual DB load using PostgreSQL metrics (`pg_stat_statements`) and Wolverine table activity in schema `wolverine`.

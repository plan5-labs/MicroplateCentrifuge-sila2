using Grpc.Core;
using Grpc.Net.Client;
using MicroplateCentrifugeCommandExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sila2.Org.Riken.Instruments.Microplatecentrifugecontroller.V1;
using Sila2.Org.Silastandard;
using SiLA2.Commands;
using SiLA2.Server;
using SiLA2.Server.Utils;
using SiLA2.Utils;
using static Sila2.Org.Riken.Instruments.Microplatecentrifugecontroller.V1.MicroplateCentrifugeController;

namespace MicroplateCentrifugeController.Features.Services
{
    public class MicroplateCentrifugeControllerService : MicroplateCentrifugeControllerBase
    {
        private readonly SiLA2.Feature _silaFeature;
        private readonly MicroplateCentrifugeCommandExecutorService.MicroplateCentrifugeCommandExecutorServiceClient _executorClient;
        private readonly ILogger _logger;
        
        // commandManagers
        private IObservableCommandManager<OpenDoor_Parameters, OpenDoor_Responses> _openDoorCommandManager;
        private IObservableCommandManager<CloseDoor_Parameters, CloseDoor_Responses> _closeDoorCommandManager;
        private IObservableCommandManager<SpinCycle_Parameters, SpinCycle_Responses> _spinCycleCommandManager;
        private IObservableCommandManager<StopSpinCycle_Parameters, StopSpinCycle_Responses> _stopSpinCycleCommandManager;
        private IObservableCommandManager<Reset_Parameters, Reset_Responses> _resetCommandManager;
        private IObservableCommandManager<Home_Parameters, Home_Responses> _homeCommandManager;
        private IObservableCommandManager<Park_Parameters, Park_Responses> _parkCommandManager;
        private IObservableCommandManager<LoadPlate_Parameters, LoadPlate_Responses> _loadPlateCommandManager;
        private IObservableCommandManager<UnloadPlate_Parameters, UnloadPlate_Responses> _unloadPlateCommandManager;

        private const string COMMAND_IS_RUNNING = "CommandIsRunning";
        private const string NOT_INITIALIZED = "NotInitialized";
        private const string INSTRUMENT_ERROR_OCCURRED = "InstrumentErrorOccurred";
        private const string INVALID_INSTRUMENT_STATUS_ERROR = "InvalidInstrumentStatusError";

        // Use on raising CommandExecutionNotAccepted error
        private const string NOT_ACCEPTED_MESSAGE = "Another command execution is in progress.";

        public MicroplateCentrifugeControllerService(IObservableCommandManager<OpenDoor_Parameters, OpenDoor_Responses> openDoorCommandManager,
                                                     IObservableCommandManager<CloseDoor_Parameters, CloseDoor_Responses> closeDoorCommandManager,
                                                     IObservableCommandManager<SpinCycle_Parameters, SpinCycle_Responses> spinCycleCommandManager,
                                                     IObservableCommandManager<StopSpinCycle_Parameters, StopSpinCycle_Responses> stopSpinCycleCommandManager,
                                                     IObservableCommandManager<Reset_Parameters, Reset_Responses> resetCommandManager,
                                                     IObservableCommandManager<Home_Parameters, Home_Responses> homeCommandManager,
                                                     IObservableCommandManager<Park_Parameters, Park_Responses> parkCommandManager,
                                                     IObservableCommandManager<LoadPlate_Parameters, LoadPlate_Responses> loadPlateCommandManager,
                                                     IObservableCommandManager<UnloadPlate_Parameters, UnloadPlate_Responses> unloadPlateCommandManager,
                                                     ISiLA2Server silaServer,
                                                     IConfiguration configuration,
                                                     ILogger<MicroplateCentrifugeControllerService> logger)
        {
            // Load feature definition for validation
            _silaFeature = silaServer.ReadFeature(Path.Combine("Features", "MicroplateCentrifugeController-v1_0.sila.xml"));

            _openDoorCommandManager = openDoorCommandManager;
            _closeDoorCommandManager = closeDoorCommandManager;
            _spinCycleCommandManager = spinCycleCommandManager;
            _stopSpinCycleCommandManager = stopSpinCycleCommandManager;
            _resetCommandManager = resetCommandManager;
            _homeCommandManager = homeCommandManager;
            _parkCommandManager = parkCommandManager;
            _loadPlateCommandManager = loadPlateCommandManager;
            _unloadPlateCommandManager = unloadPlateCommandManager;

            _logger = logger;

            // Open the channel for MicroplateCentrifuge Command Executor
            var channel = GrpcChannel.ForAddress($"http://localhost:{configuration["CommandExecutorConfig:Port"]}/");
            _executorClient = new MicroplateCentrifugeCommandExecutorService.MicroplateCentrifugeCommandExecutorServiceClient(channel);
        }

        #region OpenDoor
        public override async Task<CommandConfirmation> OpenDoor(OpenDoor_Parameters request, ServerCallContext context)
        {
            Validation.ValidateParameter(request.BucketNumber.Value, _silaFeature, "OpenDoor", "BucketNumber");
            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var command = await _openDoorCommandManager.AddCommand(request, OpenDoorWorker);
            return command.Confirmation;
        }

        public override async Task OpenDoor_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _openDoorCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<OpenDoor_Responses> OpenDoor_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _openDoorCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        /// <summary>
        /// Invoke the OpenDoor method on the CommandExecutor.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        private OpenDoor_Responses OpenDoorWorker(IProgress<ExecutionInfo> progress, OpenDoor_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var response = _executorClient.OpenDoor(new OpenDoorRequest { BucketNumber = Convert.ToInt32(parameters.BucketNumber.Value) });
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
            return new OpenDoor_Responses();
        }
        #endregion

        #region CloseDoor
        public override async Task<CommandConfirmation> CloseDoor(CloseDoor_Parameters request, ServerCallContext context)
        {
            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var response = await _closeDoorCommandManager.AddCommand(request, CloseDoorWorker);
            return response.Confirmation;
        }

        public override async Task CloseDoor_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _closeDoorCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<CloseDoor_Responses> CloseDoor_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _closeDoorCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        /// <summary>
        /// Invoke the CloseDoor method on the CommandExecutor.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        private CloseDoor_Responses CloseDoorWorker(IProgress<ExecutionInfo> progress, CloseDoor_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var response = _executorClient.CloseDoor(new CloseDoorRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
            return new CloseDoor_Responses();
        }
        #endregion

        #region SpinCycle
        public override async Task<CommandConfirmation> SpinCycle(SpinCycle_Parameters request, ServerCallContext context)
        {
            Validation.ValidateParameter(request.VelocityPercent.Value, _silaFeature, "SpinCycle", "VelocityPercent");
            Validation.ValidateParameter(request.AccelerationPercent.Value, _silaFeature, "SpinCycle", "AccelerationPercent");
            Validation.ValidateParameter(request.DecelerationPercent.Value, _silaFeature, "SpinCycle", "DecelerationPercent");
            Validation.ValidateParameter(request.TimerMode.Value, _silaFeature, "SpinCycle", "TimerMode");
            Validation.ValidateParameter(request.Time.Value, _silaFeature, "SpinCycle", "Time");
            Validation.ValidateParameter(request.BucketNumberToLoad.Value, _silaFeature, "SpinCycle", "BucketNumberToLoad");
            Validation.ValidateParameter(request.BucketNumberToUnload.Value, _silaFeature, "SpinCycle", "BucketNumberToUnload");
            Validation.ValidateParameter(request.GripperOffsetToLoad.Value, _silaFeature, "SpinCycle", "GripperOffsetToLoad");
            Validation.ValidateParameter(request.GripperOffsetToUnload.Value, _silaFeature, "SpinCycle", "GripperOffsetToUnload");
            Validation.ValidateParameter(request.PlateHeightToLoad.Value, _silaFeature, "SpinCycle", "PlateHeightToLoad");
            Validation.ValidateParameter(request.PlateHeightToUnload.Value, _silaFeature, "SpinCycle", "PlateHeightToUnload");
            Validation.ValidateParameter(request.SpeedToLoad.Value, _silaFeature, "SpinCycle", "SpeedToLoad");
            Validation.ValidateParameter(request.SpeedToUnload.Value, _silaFeature, "SpinCycle", "SpeedToUnload");
            Validation.ValidateParameter(request.OptionsToLoad.Value, _silaFeature, "SpinCycle", "OptionsToLoad");
            Validation.ValidateParameter(request.OptionsToUnload.Value, _silaFeature, "SpinCycle", "OptionsToUnload");

            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var response = await _spinCycleCommandManager.AddCommand(request, SpinCycleWorker);
            return response.Confirmation;
        }

        public override async Task SpinCycle_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _spinCycleCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<SpinCycle_Responses> SpinCycle_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _spinCycleCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        /// <summary>
        /// Invoke the SpinCycle method on the CommandExecutor.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private SpinCycle_Responses SpinCycleWorker(IProgress<ExecutionInfo> progress, SpinCycle_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {   
                var response = _executorClient.SpinCycle(new SpinCycleRequest
                {
                    VelocityPercent = Convert.ToDouble(parameters.VelocityPercent.Value),
                    AccelerationPercent = Convert.ToDouble(parameters.AccelerationPercent.Value),
                    DecelerationPercent = Convert.ToDouble(parameters.DecelerationPercent.Value),
                    TimerMode = Convert.ToInt32(parameters.TimerMode.Value),
                    Time = Convert.ToInt32(parameters.Time.Value),
                    BucketNumberToLoad = Convert.ToInt32(parameters.BucketNumberToLoad.Value),
                    BucketNumberToUnload = Convert.ToInt32(parameters.BucketNumberToUnload.Value),
                    GripperOffsetToLoad = parameters.GripperOffsetToLoad.Value,
                    GripperOffsetToUnload = parameters.GripperOffsetToUnload.Value,
                    PlateHeightToLoad = parameters.PlateHeightToLoad.Value,
                    PlateHeightToUnload = parameters.PlateHeightToUnload.Value,
                    SpeedToLoad = Convert.ToInt32(parameters.SpeedToLoad.Value),
                    SpeedToUnload = Convert.ToInt32(parameters.SpeedToUnload.Value),
                    OptionsToLoad = Convert.ToInt32(parameters.OptionsToLoad.Value),
                    OptionsToUnload = Convert.ToInt32(parameters.OptionsToUnload.Value)
                });
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
            return new SpinCycle_Responses();
        }
        #endregion

        #region StopSpinCycle
        public override async Task<CommandConfirmation> StopSpinCycle(StopSpinCycle_Parameters request, ServerCallContext context)
        {
            Validation.ValidateParameter(request.BucketNumber.Value, _silaFeature, "StopSpinCycle", "BucketNumber");
            
            // To stop the SpinCycle command, do not reject the request for the StopSpinCycle command.
            var response = await _stopSpinCycleCommandManager.AddCommand(request, StopSpinCycleWorker);
            return response.Confirmation;
        }

        public override async Task StopSpinCycle_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _stopSpinCycleCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<StopSpinCycle_Responses> StopSpinCycle_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _stopSpinCycleCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        /// <summary>
        /// Invoke the StopSpinCycle method on the CommandExecutor.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private StopSpinCycle_Responses StopSpinCycleWorker(IProgress<ExecutionInfo> progress, StopSpinCycle_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var response = _executorClient.StopSpinCycle(new StopSpinCycleRequest { BucketNumber = Convert.ToInt32(parameters.BucketNumber.Value) });
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
            return new StopSpinCycle_Responses();
        }
        #endregion

        #region Reset
        public override async Task<CommandConfirmation> Reset(Reset_Parameters request, ServerCallContext context)
        {
            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var response = await _resetCommandManager.AddCommand(request, ResetWorker);
            return response.Confirmation;
        }

        public override async Task Reset_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _resetCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<Reset_Responses> Reset_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _resetCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        /// <summary>
        /// Invoke the Reset method on the CommandExecutor.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Reset_Responses ResetWorker(IProgress<ExecutionInfo> progress, Reset_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var response = _executorClient.Reset(new ResetRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
            return new Reset_Responses();
        }
        #endregion

        public override async Task<EnumerateProfiles_Responses> EnumerateProfiles(EnumerateProfiles_Parameters request, ServerCallContext context)
        {
            var profiles = new EnumerateProfilesResponse();
            try
            {
                profiles = await _executorClient.EnumerateProfilesAsync(new EnumerateProfilesRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            var response = new EnumerateProfiles_Responses();
            foreach (var name in profiles.ProfileNames)
            {
                response.ProfileNames.Add(new Sila2.Org.Silastandard.String { Value = name });
            }
            return response;
        }

        #region home
        public override async Task<CommandConfirmation> Home(Home_Parameters request, ServerCallContext context)
        {
            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var command = await _homeCommandManager.AddCommand(request, HomeWorker);
            return command.Confirmation;
        }

        public override async Task Home_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _homeCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<Home_Responses> Home_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _homeCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        private Home_Responses HomeWorker(IProgress<ExecutionInfo> progress, Home_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                _executorClient.Home(new HomeRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
            return new Home_Responses();
        }
        #endregion

        #region Park

        public override async Task<CommandConfirmation> Park(Park_Parameters request, ServerCallContext context)
        {
            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var command = await _parkCommandManager.AddCommand(request, ParkWorker);
            return command.Confirmation;
        }

        public override async Task Park_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _parkCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<Park_Responses> Park_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _parkCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        private Park_Responses ParkWorker(IProgress<ExecutionInfo> progress, Park_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                _executorClient.Park(new ParkRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
            return new Park_Responses();
        }
        #endregion

        #region LoadPlate
        public override async Task<CommandConfirmation> LoadPlate(LoadPlate_Parameters request, ServerCallContext context)
        {
            Validation.ValidateParameter(request.BucketNumber.Value, _silaFeature, "LoadPlate", "BucketNumber");
            Validation.ValidateParameter(request.GripperOffset.Value, _silaFeature, "LoadPlate", "GripperOffset");
            Validation.ValidateParameter(request.PlateHeight.Value, _silaFeature, "LoadPlate", "PlateHeight");
            Validation.ValidateParameter(request.Speed.Value, _silaFeature, "LoadPlate", "Speed");
            Validation.ValidateParameter(request.Options.Value, _silaFeature, "LoadPlate", "Options");

            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var command = await _loadPlateCommandManager.AddCommand(request, LoadPlateWorker);
            return command.Confirmation;
        }

        public override async Task LoadPlate_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _loadPlateCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<LoadPlate_Responses> LoadPlate_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _loadPlateCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }
        
        private LoadPlate_Responses LoadPlateWorker(IProgress<ExecutionInfo> progress, LoadPlate_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var request = new LoadPlateRequest
                {
                    BucketNumber = (int)parameters.BucketNumber.Value,
                    GripperOffset = parameters.GripperOffset.Value,
                    PlateHeight = parameters.PlateHeight.Value,
                    Speed = (int)parameters.Speed.Value,
                    Options = (int)parameters.Options.Value
                };
                _executorClient.LoadPlateAsync(request);
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new LoadPlate_Responses();
        }
        #endregion

        #region UnloadPlate
        public override async Task<CommandConfirmation> UnloadPlate(UnloadPlate_Parameters request, ServerCallContext context)
        {
            Validation.ValidateParameter(request.BucketNumber.Value, _silaFeature, "UnloadPlate", "BucketNumber");
            Validation.ValidateParameter(request.GripperOffset.Value, _silaFeature, "UnloadPlate", "GripperOffset");
            Validation.ValidateParameter(request.PlateHeight.Value, _silaFeature, "UnloadPlate", "PlateHeight");
            Validation.ValidateParameter(request.Speed.Value, _silaFeature, "UnloadPlate", "Speed");
            Validation.ValidateParameter(request.Options.Value, _silaFeature, "UnloadPlate", "Options");
        
            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var command = await _unloadPlateCommandManager.AddCommand(request, UnloadPlateWorker);
            return command.Confirmation;
        }

        public override async Task UnloadPlate_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _unloadPlateCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<UnloadPlate_Responses> UnloadPlate_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _unloadPlateCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }
        
        private UnloadPlate_Responses UnloadPlateWorker(IProgress<ExecutionInfo> progress, UnloadPlate_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var request = new UnloadPlateRequest
                {
                    BucketNumber = (int)parameters.BucketNumber.Value,
                    GripperOffset = parameters.GripperOffset.Value,
                    PlateHeight = parameters.PlateHeight.Value,
                    Speed = (int)parameters.Speed.Value,
                    Options = (int)parameters.Options.Value
                };
                _executorClient.UnloadPlateAsync(request);
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
            return new UnloadPlate_Responses();
        }
        #endregion

        #region Properties
        public override async Task<Get_ActiveXVersion_Responses> Get_ActiveXVersion(Get_ActiveXVersion_Parameters request, ServerCallContext context)
        {
            var response = new GetActiveXVersionResponse();
            try
            {
                response = await _executorClient.GetActiveXVersionAsync(new GetActiveXVersionRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_ActiveXVersion_Responses { ActiveXVersion = new Sila2.Org.Silastandard.String { Value = response.ActiveXVersion } };
        }

        public override async Task<Get_CentrifugeActiveXVersion_Responses> Get_CentrifugeActiveXVersion(Get_CentrifugeActiveXVersion_Parameters request, ServerCallContext context)
        {
            var response = new GetCentrifugeActiveXVersionResponse();
            try
            {
                response = await _executorClient.GetCentrifugeActiveXVersionAsync(new GetCentrifugeActiveXVersionRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_CentrifugeActiveXVersion_Responses { CentrifugeActiveXVersion = new Sila2.Org.Silastandard.String { Value = response.CentrifugeActiveXVersion } };
        }

        public override async Task<Get_CentrifugeHardwareVersion_Responses> Get_CentrifugeHardwareVersion(Get_CentrifugeHardwareVersion_Parameters request, ServerCallContext context)
        {
            var response = new GetCentrifugeHardwareVersionResponse();
            try
            {
                response = await _executorClient.GetCentrifugeHardwareVersionAsync(new GetCentrifugeHardwareVersionRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_CentrifugeHardwareVersion_Responses { CentrifugeHardwareVersion = new Sila2.Org.Silastandard.String { Value = response.CentrifugeHardwareVersion } };
        }

        public override async Task<Get_FirmwareVersion_Responses> Get_FirmwareVersion(Get_FirmwareVersion_Parameters request, ServerCallContext context)
        {
            var response = new GetFirmwareVersionResponse();
            try
            {
                response = await _executorClient.GetFirmwareVersionAsync(new GetFirmwareVersionRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_FirmwareVersion_Responses { FirmwareVersion = new Sila2.Org.Silastandard.String { Value = response.FirmwareVersion } };
        }

        public override async Task<Get_HardwareVersion_Responses> Get_HardwareVersion(Get_HardwareVersion_Parameters request, ServerCallContext context)
        {
            var response = new GetHardwareVersionResponse();
            try
            {
                response = await _executorClient.GetHardwareVersionAsync(new GetHardwareVersionRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_HardwareVersion_Responses { HardwareVersion = new Sila2.Org.Silastandard.String { Value = response.HardwareVersion } };
        }

        public override async Task Subscribe_Status(Subscribe_Status_Parameters request, IServerStreamWriter<Subscribe_Status_Responses> responseStream, ServerCallContext context)
        {
            var cts = new CancellationTokenSource();
            var callToExecutor = _executorClient.SubscribeStatus(new SubscribeStatusRequest(), cancellationToken: cts.Token);
            try
            {
                await foreach (var update in callToExecutor.ResponseStream.ReadAllAsync(cts.Token))
                {
                    // Notify that the status changed to SiLA Client 
                    await responseStream.WriteAsync(new Subscribe_Status_Responses { Status = new Integer { Value = update.Status } });
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Subscribe Status is cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
        }
        #endregion

        private string GetErrorType(RpcException exception)
        {
            switch (exception.StatusCode)
            {
                case StatusCode.Unavailable:
                    return COMMAND_IS_RUNNING;

                case StatusCode.FailedPrecondition:
                    return exception.Message.Contains("InvalidStatus") ? INVALID_INSTRUMENT_STATUS_ERROR : NOT_INITIALIZED;

                case StatusCode.Internal:
                default:
                    return INSTRUMENT_ERROR_OCCURRED;
            }
        }

        private bool IsObservableCommandRunning()
        {
            return _openDoorCommandManager.IsCommandMapBusy ||
                   _closeDoorCommandManager.IsCommandMapBusy ||
                   _spinCycleCommandManager.IsCommandMapBusy ||
                   _stopSpinCycleCommandManager.IsCommandMapBusy ||
                   _resetCommandManager.IsCommandMapBusy ||
                   _homeCommandManager.IsCommandMapBusy ||
                   _parkCommandManager.IsCommandMapBusy ||
                   _loadPlateCommandManager.IsCommandMapBusy ||
                   _unloadPlateCommandManager.IsCommandMapBusy;
        }
    }
}

# coding: utf-8

###
# PARAMS
###

UNITY_BUILD_PARAMS = {
    unityAppPath:   "/Applications/Unity/Unity.app",
}

###
# LANES
###

def lane_release_unity(lane_context)
    app_home = get_app_home()
    release_unity(app_home)
end

###
# PRIVATE METHODS
###

def release_unity(app_home)
  out_home = File.join(app_home, "output")
  unity_app_path = lane_context[:unityAppPath]
  FileUtils.rm_rf(out_home)
  unity_execute_method(unity_app_path, app_home, "android", "UniSaga.Build.ReleaseHelper.Release")
end

def get_app_home()
  File.expand_path(File.join(__dir__, ".."))
end

def unity_execute_method(unity_home, project_path, build_target, execute_method)
  unity({
    executable:         File.join(unity_home, "Contents/MacOS/Unity"),
    project_path:       project_path,
    execute_method:     execute_method,
    build_target:       build_target,
  })
end

def unity_set_parameters(lane_context, options, hash)
  # default parameters
  hash.each do |key,value|
    lane_context[key] = value
  end

  # parameters from enviroment variables
  hash.each_key do |key|
    env_key = key.to_s # convert symbol to string
    next unless ENV.has_key?(env_key)
    case ENV[env_key]
    when 'true'
      lane_context[key] = true
    when 'false'
      lane_context[key] = false
    else
      lane_context[key] = ENV[env_key]
    end
  end

  # parameters from command arguments
  hash.each_key do |key|
    lane_context[key] = options[key] if options.has_key?(key)
  end
end
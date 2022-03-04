# Adapted from https://gist.github.com/dddnuts/522302dc0b787896ebd103542372f9c1

module Fastlane
  module Actions
    module SharedValues
    end

    class UnityAction < Action
      def self.run(params)
        UI.message Terminal::Table.new(title: "Unity", headings: ["Option", "Value"], rows: params.values)
        command  = []
        command << params[:executable]
        command << "-quit"
        command << "-batchmode"
        command << "-logFile "       + params[:log_file]
        command << "-projectPath "   + params[:project_path]
        command << "-executeMethod " + params[:execute_method]
        command << "-buildTarget "   + params[:build_target]
        sh command.join(" ")
        UI.success "Completed"
      end

      #####################################################
      # @!group Documentation
      #####################################################

      def self.description
        "UnityAction Description"
      end

      def self.details
        "UnityAction Details"
      end

      def self.available_options
        [
          FastlaneCore::ConfigItem.new(key: :executable,
                                       env_name: "FL_UNITY_EXECUTABLE",
                                       description: "Unity Executable",
                                       default_value: "/Applications/Unity/Unity.app/Contents/MacOS/Unity"),
          FastlaneCore::ConfigItem.new(key: :log_file,
                                       env_name: "FL_UNITY_LOG_FILE",
                                       description: "Log File",
                                       default_value: "-"),
          FastlaneCore::ConfigItem.new(key: :project_path,
                                       env_name: "FL_UNITY_PROJECT_PATH",
                                       description: "Unity Project Path"),
          FastlaneCore::ConfigItem.new(key: :execute_method,
                                       env_name: "FL_UNITY_EXECUTE_METHOD",
                                       description: "Unity Execute Method"),
          FastlaneCore::ConfigItem.new(key: :build_target,
                                       env_name: "FL_UNITY_BUILD_TARGET",
                                       description: "Unity Build Target"),
        ]
      end

      def self.output
        []
      end

      def self.return_value
      end

      def self.authors
        [""]
      end

      def self.is_supported?(platform)
        [:ios, :android].include?(platform)
      end
    end
  end
end
#!/usr/bin/env groovy

pipeline {
	agent any

	environment {
		CDP_BUILD_TYPE = 'Official'
		LIFTR_ENV = 'JENKINS'
	}

	stages {
		stage('restore') {
			options {
				azureKeyVault([[envVariable: 'CDP_DEFAULT_CLIENT_PACKAGE_PAT', name: 'nuget-pat', secretType: 'Secret']])
			}

			steps {
				sh './build/liftr-run-linux-restore.sh'
			}
		}

		stage('build') {
			steps {
				sh './build/liftr-run-linux-build.sh'
			}
		}

		stage('test') {
			options {
				azureKeyVault([[envVariable: 'LIFTR_CICD_AUTH_FILE_BASE64', name: 'datadog-cicd-spn-ms-auth-base64', secretType: 'Secret']])
			}

			steps {
				sh './build/liftr-run-linux-tests.sh'
			}
		}
	}
}
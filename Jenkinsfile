#!/usr/bin/env groovy

pipeline {
	agent any

	options {
		timeout(time: 100, unit: 'MINUTES')
	}

	environment {
		CDP_BUILD_TYPE = 'Official'
		LIFTR_ENV = 'JENKINS'
		LIFTR_APPINSIGHTS_IKEY = 'f3f08c6d-bfdf-49df-86be-d5444dbf3627' // https://portal.azure.com/?feature.customportal=false#@microsoft.onmicrosoft.com/resource/subscriptions/eebfbfdb-4167-49f6-be43-466a6709609f/resourcegroups/liftr-dev-wus-rg/providers/microsoft.insights/components/common-cicd-logs-wus2/overview
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